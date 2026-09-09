using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Palabravo.Core.Models;
using Palabravo.Core.Services;
using Palabravo.Services;

namespace Palabravo.ViewModels;

public partial class RankingViewModel(WeeklyChallengeService weeklyChallenges, ProgressService progress) : ObservableObject
{
    public ObservableCollection<RankingEntryViewModel> Entries { get; } = [];

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasEntries;
    [ObservableProperty] private bool showEmpty;
    [ObservableProperty] private bool showError;
    [ObservableProperty] private bool showMyPosition;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string nextResetText = "Se renueva semanalmente";
    [ObservableProperty] private string myRank = "—";
    [ObservableProperty] private string myName = "Aún no participas";
    [ObservableProperty] private string myResult = "Completa el reto semanal para aparecer";
    [ObservableProperty] private string heading = "Ranking semanal";
    [ObservableProperty] private string emptyTitle = "Sé el primero esta semana";
    [ObservableProperty] private string emptyText = "Completa el reto semanal y tu mejor resultado aparecerá aquí.";

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ShowError = false;
        ShowEmpty = false;

        var weekly = await weeklyChallenges.GetCurrentAsync();
        if (weekly is null)
        {
            Entries.Clear();
            HasEntries = false;
            ShowMyPosition = false;
            ShowError = true;
            ErrorMessage = "No hay un reto semanal activo en este momento.";
            IsLoading = false;
            return;
        }

        Heading = $"{weekly.Flag} Ranking semanal";
        var savedProgress = await progress.LoadAsync();
        if (savedProgress.Completions.TryGetValue($"weekly:{weekly.Puzzle.Id}", out var completion))
            weeklyChallenges.QueueStoredResult(weekly, completion);
        await LoadWeeklyAsync(weekly);
        IsLoading = false;
    }

    private async Task LoadWeeklyAsync(WeeklyChallengeDefinition weekly)
    {
        var snapshot = await weeklyChallenges.GetRankingAsync(weekly.Id);
        Entries.Clear();
        foreach (var entry in snapshot.Entries)
            Entries.Add(ToViewModel(entry.Rank, entry.Name, entry.Score, entry.IsCurrentPlayer));
        HasEntries = Entries.Count > 0;
        ShowEmpty = snapshot.IsAvailable && !HasEntries;
        ShowError = !snapshot.IsAvailable;
        ErrorMessage = snapshot.Error ?? string.Empty;
        NextResetText = $"Termina {FormatReset(weekly.EndsAt.ToLocalTime())}";
        ShowMyPosition = snapshot.CurrentPlayer is not null;
        if (snapshot.CurrentPlayer is { } current)
        {
            MyRank = $"#{current.Rank}";
            MyName = current.Name;
            MyResult = $"{FormatDetails(LeaderboardScore.Decode(current.Score))} · Top {snapshot.Percentile}%";
        }
        else
        {
            MyRank = "—"; MyName = "Aún no participas"; MyResult = "Completa el reto semanal para aparecer";
        }
    }

    private static RankingEntryViewModel ToViewModel(int rank, string name, long score, bool isCurrentPlayer)
    {
        var details = LeaderboardScore.Decode(score);
        return new RankingEntryViewModel(
            $"#{rank}",
            name,
            MedalIcon(details.Medal),
            FormatDetails(details),
            isCurrentPlayer ? "#FCE4DE" : "#FFFDFC",
            rank <= 3 ? "#EB5B43" : "#6D6964");
    }

    private static string FormatDetails(LeaderboardScoreDetails details) =>
        $"{details.Errors} err. · {details.Hints} pistas · {(int)details.Elapsed.TotalMinutes:00}:{details.Elapsed.Seconds:00}";

    private static string MedalIcon(Medal medal) => medal switch
    {
        Medal.Gold => "🥇",
        Medal.Silver => "🥈",
        Medal.Bronze => "🥉",
        _ => "✦"
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
    string Rank,
    string Name,
    string MedalIcon,
    string Details,
    string BackgroundColor,
    string RankColor);
