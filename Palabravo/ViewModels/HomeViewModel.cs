using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Palabravo.Core.Models;
using Palabravo.Core.Services;
using Palabravo.Services;

namespace Palabravo.ViewModels;

public partial class HomeViewModel(IPuzzleRepository puzzles, ProgressService progress, IClock clock,
    WeeklyChallengeService weeklyChallenges, Palabravo.Core.Monetization.IMonetizationTelemetry telemetry, IPlayerAccountService accountService) : ObservableObject
{
    private string _nextPuzzleId = "puzzle-01";
    private string? _weeklyId;
    private bool weeklyShownTracked;

    [ObservableProperty] private string dailyTitle = "Reto diario";
    [ObservableProperty] private string dailyDifficulty = "Preparando tu reto…";
    [ObservableProperty] private string streakText = "0";
    [ObservableProperty] private string streakCaption = "Empieza hoy tu racha";
    [ObservableProperty] private string rankText = "Novato";
    [ObservableProperty] private string progressText = "0 de 5 retos";
    [ObservableProperty] private double rankProgress;
    [ObservableProperty] private string nextChallengeText = "Comenzar el camino";
    [ObservableProperty] private bool hasWeekly;
    [ObservableProperty] private string weeklyFlag = "✦";
    [ObservableProperty] private string? weeklyBadgeImageSource;
    [ObservableProperty] private string weeklyTitle = "Reto de la semana";
    [ObservableProperty] private string weeklySubtitle = string.Empty;
    [ObservableProperty] private string weeklyEndsText = string.Empty;

    [ObservableProperty] private bool showGoogleSignIn;
    [ObservableProperty] private bool isAccountBusy;

    public void RefreshAccount()
    {
        var account = accountService.GetSnapshot();
        ShowGoogleSignIn = account.CanUseGooglePlayGames && !account.IsGoogleLinked;
        ContinueWithGoogleCommand.NotifyCanExecuteChanged();
    }

    private bool CanContinueWithGoogle() => ShowGoogleSignIn && !IsAccountBusy;

    [RelayCommand(CanExecute = nameof(CanContinueWithGoogle))]
    private async Task ContinueWithGoogleAsync()
    {
        IsAccountBusy = true;
        ContinueWithGoogleCommand.NotifyCanExecuteChanged();
        try
        {
            var result = await accountService.ContinueWithGoogleAsync();
            await AccountFeedback.ShowAsync(result.Message);
            RefreshAccount();
        }
        finally
        {
            IsAccountBusy = false;
            ContinueWithGoogleCommand.NotifyCanExecuteChanged();
        }
    }

    public async Task RefreshAsync()
    {
        RefreshAccount();
        var state = await progress.LoadAsync();
        var all = await puzzles.GetAllAsync();
        var daily = await puzzles.GetDailyAsync(clock.UtcToday);
        DailyTitle = daily.Title;
        DailyDifficulty = $"{daily.Difficulty} · cambia a las 00:00 UTC";
        StreakText = state.CurrentStreak.ToString();
        StreakCaption = state.CurrentStreak == 1 ? "día seguido" : "días seguidos";
        var rankProgress = state.RankProgress;
        RankText = rankProgress.Definition.Name;
        RankProgress = rankProgress.Fraction;
        ProgressText = rankProgress.IsPathComplete
            ? "Camino completado"
            : $"{rankProgress.CompletedInRank} de {rankProgress.RequiredInRank} retos";

        var next = all.FirstOrDefault(x => !state.Completions.ContainsKey($"challenge:{x.Id}")) ?? all[^1];
        _nextPuzzleId = next.Id;
        NextChallengeText = all.All(p => state.Completions.ContainsKey($"challenge:{p.Id}"))
            ? $"Repetir el reto {next.Order}" : $"{next.DynamicLabel} · Reto {next.Order}: {next.Title}";

        var weekly = await weeklyChallenges.GetCurrentAsync();
        HasWeekly = weekly is not null && !state.HasCompletedWeekly(weekly.Puzzle.Id);
        _weeklyId = HasWeekly ? weekly?.Id : null;
        if (weekly is not null && HasWeekly)
        {
            WeeklyFlag = weekly.Flag;
            WeeklyBadgeImageSource = weekly.Badge.ImageUrl;
            WeeklyTitle = weekly.Title;
            WeeklySubtitle = weekly.Subtitle;
            var days = Math.Max(1, (int)Math.Ceiling((weekly.EndsAt - DateTimeOffset.UtcNow).TotalDays));
            WeeklyEndsText = days == 1 ? "Termina mañana" : $"Termina en {days} días";
            if (!weeklyShownTracked)
            {
                weeklyShownTracked = true;
                telemetry.Track("weekly_shown", new Dictionary<string, object> { ["weekly_id"] = weekly.Id });
            }
        }
    }

    [RelayCommand]
    private async Task OpenChallengeCodeAsync()
    {
        var code = await Shell.Current.DisplayPromptAsync("Reto de un amigo", "Ingresa el código del reto (por ejemplo, 07).", "Abrir", "Cancelar", maxLength: 20);
        if (code is null) return;
        var id = ReferralLink.PuzzleFromCode(code);
        if (id is null || await puzzles.GetByIdAsync(id) is null) { await Shell.Current.DisplayAlertAsync("Código no válido", "Ingresa el código de un reto disponible.", "Cerrar"); return; }
        await Shell.Current.GoToAsync(nameof(Views.GamePage), new Dictionary<string, object>
        { ["puzzleId"] = id, ["mode"] = "Challenge", ["referred"] = true });
    }

    [RelayCommand]
    private Task PlayDailyAsync() => Shell.Current.GoToAsync(nameof(Views.GamePage), new Dictionary<string, object>
    {
        ["puzzleId"] = "daily",
        ["mode"] = PuzzleMode.Daily.ToString()
    });

    [RelayCommand]
    private Task PlayWeeklyAsync() => _weeklyId is null ? Task.CompletedTask :
        Shell.Current.GoToAsync(nameof(Views.GamePage), new Dictionary<string, object>
        {
            ["puzzleId"] = _weeklyId,
            ["mode"] = PuzzleMode.Weekly.ToString()
        });

    [RelayCommand]
    private Task PlayNextAsync() => Shell.Current.GoToAsync(nameof(Views.GamePage), new Dictionary<string, object>
    {
        ["puzzleId"] = _nextPuzzleId,
        ["mode"] = PuzzleMode.Challenge.ToString()
    });
}
