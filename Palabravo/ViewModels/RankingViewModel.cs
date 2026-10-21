using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Palabravo.Core.Models;
using Palabravo.Core.Services;
using Palabravo.Services;

namespace Palabravo.ViewModels;

public partial class RankingViewModel(
    WeeklyChallengeService weeklyChallenges,
    PathRankingService pathRanking,
    ProgressService progress,
    IPlayerAccountService accountService) : ObservableObject
{
    public ObservableCollection<RankingEntryViewModel> Entries { get; } = [];
    private bool showingWeekly;
    private string? activeWeeklyId;
    public bool CanSelectMode => !IsLoading;
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(CanSelectMode));

    public void SetMode(bool weekly)
    {
        showingWeekly = weekly;
        UpdateTabs();
    }

    [ObservableProperty] private bool showParticipationGate;
    [ObservableProperty] private string participationTitle = string.Empty;
    [ObservableProperty] private string participationText = string.Empty;
    [ObservableProperty] private string playButtonText = "Jugar un reto";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasEntries;
    [ObservableProperty] private bool showEmpty;
    [ObservableProperty] private bool showError;
    [ObservableProperty] private bool showMyPosition;
    [ObservableProperty] private bool showSyncWarning;
    [ObservableProperty] private string syncMessage = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string nextResetText = "Tu mejor resultado por reto";
    [ObservableProperty] private string myRank = "—";
    [ObservableProperty] private string myName = "Aún no participas";
    [ObservableProperty] private string myResult = "Completa un reto del mapa para aparecer";
    [ObservableProperty] private string heading = "Ranking del camino";
    [ObservableProperty] private string description = "Cada reto aporta 100 puntos más un bono por tu mejor medalla.";
    [ObservableProperty] private string emptyTitle = "Sé el primero en el camino";
    [ObservableProperty] private string emptyText = "Completa un reto del mapa y tu puntaje aparecerá aquí.";
    [ObservableProperty] private string pathTabColor = "#EB5B43";
    [ObservableProperty] private string pathTabTextColor = "#FFFFFF";
    [ObservableProperty] private string weeklyTabColor = "#FFFDFC";
    [ObservableProperty] private string weeklyTabTextColor = "#6D6964";

    [ObservableProperty] private bool showGoogleSignIn;
    [ObservableProperty] private bool isAccountBusy;

    private void RefreshAccount()
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
            if (result.IsSuccess) await LoadAsync();
        }
        finally
        {
            IsAccountBusy = false;
            ContinueWithGoogleCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        RefreshAccount();
        if (IsLoading) return;
        IsLoading = true;
        ClearRanking();
        ShowParticipationGate = false;
        ShowError = false;
        ShowEmpty = false;
        ShowSyncWarning = false;
        try
        {
            if (showingWeekly) await LoadWeeklyAsync();
            else await LoadPathAsync();
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SelectPathAsync()
    {
        if (IsLoading || !showingWeekly) return;
        showingWeekly = false;
        UpdateTabs();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SelectWeeklyAsync()
    {
        if (IsLoading || showingWeekly) return;
        showingWeekly = true;
        UpdateTabs();
        await LoadAsync();
    }

    private async Task LoadPathAsync()
    {
        Heading = "Ranking del camino";
        Description = "Cada reto aporta 100 puntos más un bono por tu mejor medalla.";
        NextResetText = "Cuenta tu mejor resultado en cada reto";
        EmptyTitle = "Sé el primero en el camino";
        EmptyText = "Completa un reto del mapa y tu puntaje aparecerá aquí.";
        var savedProgress = await progress.LoadAsync();
        if (!RankingParticipation.CanViewPath(savedProgress))
        {
            ShowGate("Juega para descubrir el ranking",
                "Completa tu primer reto del camino, diario o semanal para ver la clasificación completa.", "Jugar un reto");
            return;
        }
        var snapshot = await pathRanking.GetRankingAsync(savedProgress);
        Entries.Clear();
        foreach (var entry in snapshot.Entries) Entries.Add(ToPathViewModel(entry));
        ApplyState(snapshot.IsAvailable, snapshot.Error);
        ShowSyncWarning = snapshot.PendingSync;
        SyncMessage = "Tu puntaje está guardado en el dispositivo y se enviará automáticamente al recuperar la conexión.";
        ShowMyPosition = snapshot.CurrentPlayer is not null;
        if (snapshot.CurrentPlayer is { } current)
        {
            MyRank = $"#{current.Rank}";
            MyName = current.Name;
            var target = snapshot.PointsToNext is { } points ? $" · faltan {points} pts para subir" : string.Empty;
            MyResult = $"{current.Score} pts · {current.Completed} retos · Top {snapshot.Percentile}%{target}";
        }
        else
        {
            MyRank = "—"; MyName = "Aún no participas";
            MyResult = "Completa un reto del mapa para aparecer";
        }
    }

    private async Task LoadWeeklyAsync()
    {
        var weekly = await weeklyChallenges.GetCurrentAsync();
        activeWeeklyId = weekly?.Id;
        if (weekly is null)
        {
            Entries.Clear(); HasEntries = false; ShowMyPosition = false; ShowEmpty = false; ShowError = true;
            Heading = "Ranking semanal";
            Description = "Todos compiten resolviendo el mismo reto.";
            NextResetText = "No hay un reto semanal activo";
            ErrorMessage = "No hay un reto semanal activo en este momento.";
            return;
        }

        Heading = $"{weekly.Flag} Ranking semanal";
        Description = "Todos compiten resolviendo el mismo reto; cuenta tu mejor resultado.";
        EmptyTitle = "Sé el primero esta semana";
        EmptyText = "Completa el reto semanal y tu mejor resultado aparecerá aquí.";
        NextResetText = $"Termina {FormatReset(weekly.EndsAt.ToLocalTime())}";
        var savedProgress = await progress.LoadAsync();
        if (!RankingParticipation.CanViewWeekly(savedProgress, weekly.Puzzle.Id))
        {
            ShowGate("Entra al ranking de esta semana",
                "Completa el reto semanal para descubrir tu posición y ver la clasificación completa. Cada semana empieza una nueva competencia.",
                "Jugar el reto semanal");
            return;
        }
        if (savedProgress.Completions.TryGetValue($"weekly:{weekly.Puzzle.Id}", out var completion))
            weeklyChallenges.QueueStoredResult(weekly, completion);
        var snapshot = await weeklyChallenges.GetRankingAsync(weekly.Id);
        Entries.Clear();
        foreach (var entry in snapshot.Entries)
            Entries.Add(ToWeeklyViewModel(entry.Rank, entry.Name, entry.Score, entry.IsCurrentPlayer));
        ApplyState(snapshot.IsAvailable, snapshot.Error);
        ShowSyncWarning = snapshot.PendingSync;
        SyncMessage = "Tu resultado semanal está guardado en el dispositivo y se reintentará automáticamente.";
        ShowMyPosition = snapshot.CurrentPlayer is not null;
        if (snapshot.CurrentPlayer is { } current)
        {
            MyRank = $"#{current.Rank}";
            MyName = current.Name;
            MyResult = $"{FormatDetails(LeaderboardScore.Decode(current.Score))} · Top {snapshot.Percentile}%";
        }
        else
        {
            MyRank = "—"; MyName = "Aún no participas";
            MyResult = "Completa el reto semanal para aparecer";
        }
    }

    private void ClearRanking()
    {
        Entries.Clear();
        HasEntries = false;
        ShowMyPosition = false;
        ShowEmpty = false;
        ShowError = false;
        ShowSyncWarning = false;
    }

    private void ShowGate(string title, string text, string button)
    {
        ClearRanking();
        ParticipationTitle = title;
        ParticipationText = text;
        PlayButtonText = button;
        ShowParticipationGate = true;
    }

    [RelayCommand]
    private Task PlayToParticipateAsync()
    {
        if (IsLoading || !ShowParticipationGate) return Task.CompletedTask;
        if (!showingWeekly) return Shell.Current.GoToAsync("//main/challenges");
        return activeWeeklyId is null ? Task.CompletedTask :
            Shell.Current.GoToAsync(nameof(Views.GamePage), new Dictionary<string, object>
            {
                ["puzzleId"] = activeWeeklyId,
                ["mode"] = PuzzleMode.Weekly.ToString()
            });
    }

    private void ApplyState(bool available, string? error)
    {
        HasEntries = Entries.Count > 0;
        ShowEmpty = available && !HasEntries;
        ShowError = !available;
        ErrorMessage = error ?? string.Empty;
    }

    private void UpdateTabs()
    {
        PathTabColor = showingWeekly ? "#FFFDFC" : "#EB5B43";
        PathTabTextColor = showingWeekly ? "#6D6964" : "#FFFFFF";
        WeeklyTabColor = showingWeekly ? "#EB5B43" : "#FFFDFC";
        WeeklyTabTextColor = showingWeekly ? "#FFFFFF" : "#6D6964";
    }

    private static RankingEntryViewModel ToPathViewModel(PathRankingItem entry) => new(
        $"#{entry.Rank}", entry.Name, entry.Rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => "✦" },
        $"{entry.Score} pts · {entry.Completed} retos · {entry.Gold} oros",
        entry.IsCurrentPlayer ? "#FCE4DE" : "#FFFDFC",
        entry.Rank <= 3 ? "#EB5B43" : "#6D6964");

    private static RankingEntryViewModel ToWeeklyViewModel(int rank, string name, long score, bool isCurrentPlayer)
    {
        var details = LeaderboardScore.Decode(score);
        return new RankingEntryViewModel(
            $"#{rank}", name, MedalIcon(details.Medal), FormatDetails(details),
            isCurrentPlayer ? "#FCE4DE" : "#FFFDFC", rank <= 3 ? "#EB5B43" : "#6D6964");
    }

    private static string FormatDetails(LeaderboardScoreDetails details) =>
        $"{details.Errors} err. · {details.Hints} pistas · {(int)details.Elapsed.TotalMinutes:00}:{details.Elapsed.Seconds:00}";

    private static string MedalIcon(Medal medal) => medal switch
    {
        Medal.Gold => "🥇", Medal.Silver => "🥈", Medal.Bronze => "🥉", _ => "✦"
    };

    private static string FormatReset(DateTimeOffset reset)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var date = DateOnly.FromDateTime(reset.LocalDateTime);
        var day = date == today ? "hoy" : date == today.AddDays(1) ? "mañana" : reset.ToString("dd/MM");
        return $"{day} a las {reset:HH:mm}";
    }
}

public sealed record RankingEntryViewModel(
    string Rank, string Name, string MedalIcon, string Details,
    string BackgroundColor, string RankColor);
