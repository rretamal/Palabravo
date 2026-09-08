using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Palabravo.Core.Models;
using Palabravo.Core.Services;
using Palabravo.Services;

namespace Palabravo.ViewModels;

public partial class RankingViewModel(ILeaderboardService leaderboard) : ObservableObject
{
    public ObservableCollection<RankingEntryViewModel> Entries { get; } = [];

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasEntries;
    [ObservableProperty] private bool showEmpty;
    [ObservableProperty] private bool showError;
    [ObservableProperty] private bool showMyPosition;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string nextResetText = "Se reinicia diariamente";
    [ObservableProperty] private string myRank = "—";
    [ObservableProperty] private string myName = "Aún no participas";
    [ObservableProperty] private string myResult = "Completa el reto diario para aparecer";

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        ShowError = false;
        ShowEmpty = false;

        var snapshot = await leaderboard.GetDailyAsync();
        Entries.Clear();
        foreach (var entry in snapshot.Entries)
            Entries.Add(ToViewModel(entry));

        HasEntries = Entries.Count > 0;
        ShowEmpty = snapshot.IsAvailable && !HasEntries;
        ShowError = !snapshot.IsAvailable;
        ErrorMessage = snapshot.Error ?? string.Empty;
        NextResetText = snapshot.NextReset is { } reset
            ? $"Reinicia {FormatReset(reset.ToLocalTime())}"
            : "Se reinicia diariamente";

        ShowMyPosition = snapshot.CurrentPlayer is not null;
        if (snapshot.CurrentPlayer is { } current)
        {
            var details = LeaderboardScore.Decode(current.Score);
            MyRank = $"#{current.Rank}";
            MyName = current.Name;
            MyResult = FormatDetails(details);
        }
        else
        {
            MyRank = "—";
            MyName = "Aún no participas";
            MyResult = "Completa el reto diario para aparecer";
        }

        IsLoading = false;
    }

    private static RankingEntryViewModel ToViewModel(LeaderboardEntry entry)
    {
        var details = LeaderboardScore.Decode(entry.Score);
        return new RankingEntryViewModel(
            $"#{entry.Rank}",
            entry.Name,
            MedalIcon(details.Medal),
            FormatDetails(details),
            entry.IsCurrentPlayer ? "#FCE4DE" : "#FFFDFC",
            entry.Rank <= 3 ? "#EB5B43" : "#6D6964");
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
