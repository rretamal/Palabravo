using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Palabravo.Core.Models;
using Palabravo.Core.Services;
using Palabravo.Services;
using Palabravo.Core.Monetization;
using Palabravo.Services.Monetization;

namespace Palabravo.ViewModels;

public partial class GameViewModel(
    GameCoordinator coordinator,
    ProgressService progress,
    IMonetizationService monetization,
    IMonetizationTelemetry telemetry,
    MonetizationOfferPresenter offers,
    GameplayActivity activity,
    WeeklyChallengeService weeklyChallenges) : ObservableObject
{
    private static readonly string[] GroupColors = ["#FCE4DE", "#FFF0C8", "#DDEFE9", "#E5E9F5"];
    private bool referredAttempt;

    public ObservableCollection<WordTileViewModel> Words { get; } = [];
    public ObservableCollection<SolvedGroupViewModel> SolvedGroups { get; } = [];
    public event EventHandler<SubmissionFeedbackEventArgs>? SubmissionFeedbackRequested;

    [ObservableProperty] private string title = "Reto";
    [ObservableProperty] private string subtitle = "Cuatro grupos. Dieciséis palabras.";
    [ObservableProperty] private string attemptDots = "● ● ●";
    [ObservableProperty] private string errorText = $"0 / {PuzzleEngine.MaxErrors}";
    [ObservableProperty] private string hintText = "2 pistas";
    [ObservableProperty] private string instructions = string.Empty;
    [ObservableProperty] private string statusMessage = "Selecciona cuatro palabras relacionadas";
    [ObservableProperty] private bool canSubmit;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isGameplayActive;
    partial void OnIsGameplayActiveChanged(bool value) => SubmitCommand.NotifyCanExecuteChanged();

    public async Task LoadAsync(string puzzleId, string modeText, bool referred = false)
    {
        if (!Enum.TryParse<PuzzleMode>(modeText, true, out var mode))
            mode = PuzzleMode.Challenge;

        referredAttempt = referred;
        await coordinator.StartAsync(puzzleId, mode, referred);
        activity.Touch();
        RefreshFromEngine();
        if (mode == PuzzleMode.Weekly)
            telemetry.Track(referred ? "challenge_started" : "weekly_started",
                new Dictionary<string, object> { ["weekly_id"] = coordinator.CurrentWeekly?.Id ?? puzzleId });

        var state = await progress.LoadAsync();
        if (!state.TutorialSeen)
        {
            await progress.MarkTutorialSeenAsync();
            await Shell.Current.GoToAsync(nameof(Views.TutorialPage));
        }
    }

    public void RefreshFromEngine()
    {
        var state = coordinator.Engine.State;
        if (state is null)
            return;

        Title = state.Mode switch
        {
            PuzzleMode.Daily => "Reto diario",
            PuzzleMode.Weekly => $"{coordinator.CurrentWeekly?.Flag} Reto semanal",
            _ => $"Reto {state.Puzzle.Order}"
        };
        Subtitle = state.Puzzle.Title;
        Instructions = state.Puzzle.Instructions;
        StatusMessage = "Selecciona cuatro palabras relacionadas";
        RebuildCollections();
    }

    [RelayCommand]
    private void ToggleWord(WordTileViewModel tile)
    {
        if (IsBusy || !IsGameplayActive) return;
        activity.Touch();
        if (!coordinator.Engine.ToggleWord(tile.Word))
            return;

        tile.IsSelected = !tile.IsSelected;
        var selected = coordinator.Engine.State?.SelectedWords.Count ?? 0;
        CanSubmit = selected == 4;
        StatusMessage = selected == 4 ? "¡Listo! Comprueba tu grupo" : $"{selected} de 4 seleccionadas";
        SubmitCommand.NotifyCanExecuteChanged();
    }

    private bool CanSubmitGroup() => CanSubmit && !IsBusy && IsGameplayActive;

    [RelayCommand(CanExecute = nameof(CanSubmitGroup))]
    private async Task SubmitAsync()
    {
        IsBusy = true;
        SubmitCommand.NotifyCanExecuteChanged();
        var outcome = coordinator.Engine.Submit();

        if (outcome.Kind is SubmissionKind.Incorrect or SubmissionKind.Lost)
        {
            StatusMessage = outcome.Kind == SubmissionKind.Lost
                ? "Se acabaron los intentos"
                : "No es un grupo. Prueba otra conexión";
        }
        else
        {
            StatusMessage = outcome.Kind == SubmissionKind.Won
                ? "¡Encontraste todas las conexiones!"
                : $"Grupo encontrado: {outcome.SolvedGroup?.Category}";
        }

        RebuildCollections();
        SubmissionFeedbackRequested?.Invoke(this, new SubmissionFeedbackEventArgs(
            outcome.Kind is SubmissionKind.Correct or SubmissionKind.Won,
            outcome.Kind is SubmissionKind.Won));
        IsBusy = false;
        SubmitCommand.NotifyCanExecuteChanged();

        if (outcome.Kind is SubmissionKind.Won or SubmissionKind.Lost)
        {
            await Task.Delay(450);
            await FinishGameAsync();
        }
    }

    [RelayCommand]
    private void Shuffle()
    {
        if (IsBusy || !IsGameplayActive) return;
        activity.Touch();
        coordinator.Engine.Shuffle();
        RebuildCollections();
        StatusMessage = "Palabras mezcladas";
    }

    [RelayCommand]
    private async Task HintAsync()
    {
        if (IsBusy || !IsGameplayActive) return;
        IsBusy = true;
        SubmitCommand.NotifyCanExecuteChanged();
        using var pause = activity.Pause();
        try
        {
        var offer = monetization.GetHintOffer(coordinator.Engine.HasUsefulHint);
        var options = monetization.IsEnabled
            ? new[] { offer.Label, "Ver solución", "Palabravo sin anuncios" }
            : new[] { offer.Label, "Ver solución" };
        var sheet = Shell.Current.DisplayActionSheetAsync("Ayudas", "Cancelar", null, options);
        if (offer.Source == HintSource.Advertisement)
            telemetry.Track("reward_offer_view", new Dictionary<string, object> { ["offer_id"] = offer.OfferId, ["placement_id"] = "hint", ["reward_type"] = "partial_hint" });
        var action = await sheet;
        if (action == "Palabravo sin anuncios") { await offers.ShowAsync(); return; }

        if (action == "Ver solución")
        {
            var confirmed = await Shell.Current.DisplayAlertAsync(
                "Ver solución",
                "Esto terminará la partida y no obtendrás medalla. ¿Quieres ver las cuatro conexiones?",
                "Ver solución",
                "Seguir jugando");
            if (!confirmed || !coordinator.Engine.GiveUpAndRevealSolution())
                return;

            await FinishGameAsync();
            return;
        }

        if (action != offer.Label)
            return;

        string? hint = null;
        if (await monetization.AcceptHintAsync(offer))
            monetization.ConsumeHint(coordinator.Engine.State!.AttemptId, () => (hint = coordinator.Engine.UseHint()) is not null);
        if (hint is null)
        {
            await Shell.Current.DisplayAlertAsync("Pista no disponible", coordinator.Engine.HasUsefulHint
                ? "No hay una pista disponible ahora. Puedes seguir jugando o reintentar más tarde."
                : "Ya utilizaste las pistas disponibles para este reto.", "Cerrar");
            return;
        }

        UpdateHeader();
        await Shell.Current.DisplayAlertAsync("Una pista", hint, "Seguir jugando");
        monetization.ConfirmHintDisplayed();
        }
        finally
        {
            IsBusy = false;
            SubmitCommand.NotifyCanExecuteChanged();
            activity.Touch();
        }
    }

    private async Task FinishGameAsync()
    {
        var result = coordinator.Finish();
        var progressUpdate = await progress.RecordCompletionAsync(result);
        coordinator.SetProgressUpdate(progressUpdate);
        if (result.IsSuccess && result.Mode == PuzzleMode.Weekly && coordinator.CurrentWeekly is { } weekly)
        {
            var earned = await progress.EarnSpecialBadgeAsync(weekly.Badge);
            telemetry.Track("weekly_completed", new Dictionary<string, object> { ["weekly_id"] = weekly.Id });
            if (referredAttempt)
                telemetry.Track("challenge_completed", new Dictionary<string, object> { ["weekly_id"] = weekly.Id });
            if (earned)
                telemetry.Track("badge_earned", new Dictionary<string, object>
                    { ["weekly_id"] = weekly.Id, ["badge_id"] = weekly.Badge.Id });
            _ = weeklyChallenges.SubmitResultAsync(weekly, result);
        }
        await monetization.CompleteAttemptAsync(coordinator.Engine.State!.AttemptId, result.IsSuccess);
        await Shell.Current.GoToAsync(nameof(Views.ResultPage));
    }

    private void RebuildCollections()
    {
        var state = coordinator.Engine.State;
        if (state is null)
            return;

        Words.Clear();
        foreach (var word in state.RemainingWords)
            Words.Add(new WordTileViewModel(word, state.SelectedWords.Contains(word)));

        SolvedGroups.Clear();
        for (var i = 0; i < state.SolvedGroups.Count; i++)
        {
            var group = state.SolvedGroups[i];
            SolvedGroups.Add(new SolvedGroupViewModel(group.Category,
                string.Join(" · ", group.Words), group.Explanation, GroupColors[i % GroupColors.Length]));
        }

        CanSubmit = state.SelectedWords.Count == 4;
        SubmitCommand.NotifyCanExecuteChanged();
        UpdateHeader();
    }

    private void UpdateHeader()
    {
        var state = coordinator.Engine.State;
        if (state is null)
            return;

        AttemptDots = string.Join(" ", Enumerable.Range(0, PuzzleEngine.MaxErrors)
            .Select(i => i < PuzzleEngine.MaxErrors - state.Errors ? "●" : "○"));
        ErrorText = $"{state.Errors} / {PuzzleEngine.MaxErrors}";
        HintText = $"{2 - state.HintsUsed} {(2 - state.HintsUsed == 1 ? "pista" : "pistas")}";
    }

}

public sealed record SubmissionFeedbackEventArgs(bool IsSuccess, bool IsFinal);

public partial class WordTileViewModel(string word, bool isSelected) : ObservableObject
{
    public string Word { get; } = word;
    public double FontSize => Word.Length switch
    {
        >= 12 => 8,
        >= 10 => 9,
        >= 8 => 10,
        _ => 11
    };
    [ObservableProperty] private bool isSelected = isSelected;
    public string BackgroundColor => IsSelected ? "#EB5B43" : "#FFFDFC";
    public string TextColor => IsSelected ? "#FFFFFF" : "#242322";

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(TextColor));
    }
}

public sealed record SolvedGroupViewModel(string Category, string Words, string Explanation, string BackgroundColor);
