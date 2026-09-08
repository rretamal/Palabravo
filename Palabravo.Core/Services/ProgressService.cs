using Palabravo.Core.Models;

namespace Palabravo.Core.Services;

public sealed record ProgressUpdate(
    bool IsNewCompletion,
    bool MedalImproved,
    RankDefinition PreviousRank,
    RankDefinition CurrentRank,
    bool RankChanged,
    bool PathCompleted);

public sealed class ProgressService(IProgressStore store, IClock clock)
{
    private bool _loaded;

    public PlayerProgress Current { get; private set; } = new();
    public event EventHandler? ProgressChanged;

    public async Task<PlayerProgress> LoadAsync()
    {
        if (_loaded)
            return Current;

        Current = await store.LoadAsync();
        _loaded = true;
        return Current;
    }

    public bool IsChallengeUnlocked(PuzzleDefinition puzzle, IReadOnlyList<PuzzleDefinition> orderedPuzzles)
    {
        var index = -1;
        for (var candidate = 0; candidate < orderedPuzzles.Count; candidate++)
        {
            if (!string.Equals(orderedPuzzles[candidate].Id, puzzle.Id, StringComparison.OrdinalIgnoreCase))
                continue;

            index = candidate;
            break;
        }

        if (index < 0)
            throw new ArgumentException("El reto no pertenece al catálogo indicado.", nameof(puzzle));

        return index <= 0 || Current.Completions.ContainsKey($"challenge:{orderedPuzzles[index - 1].Id}");
    }

    public async Task MarkTutorialSeenAsync()
    {
        await LoadAsync();
        if (Current.TutorialSeen)
            return;

        Current.TutorialSeen = true;
        await SaveAsync();
    }

    public async Task ResetAsync()
    {
        await LoadAsync();
        Current = new PlayerProgress();
        await SaveAsync();
    }

    public async Task<ProgressUpdate> RecordCompletionAsync(GameResult result)
    {
        await LoadAsync();
        var previousRank = Current.RankProgress.Definition;
        if (!result.IsSuccess)
            return Unchanged(previousRank);

        var key = result.Mode == PuzzleMode.Daily
            ? $"daily:{result.PlayedOn:yyyy-MM-dd}"
            : $"challenge:{result.PuzzleId}";

        if (Current.Completions.TryGetValue(key, out var existing))
        {
            var medalImproved = result.Medal > existing.BestMedal;
            if (medalImproved)
            {
                existing.BestMedal = result.Medal;
                existing.Errors = result.Errors;
                existing.HintsUsed = result.HintsUsed;
                existing.ElapsedSeconds = result.Elapsed.TotalSeconds;
                await SaveAsync();
            }
            return new ProgressUpdate(false, medalImproved, previousRank, Current.RankProgress.Definition, false, false);
        }

        Current.Completions[key] = new CompletionRecord
        {
            Key = key,
            PuzzleId = result.PuzzleId,
            Mode = result.Mode,
            CompletedOn = result.PlayedOn,
            BestMedal = result.Medal,
            Errors = result.Errors,
            HintsUsed = result.HintsUsed,
            ElapsedSeconds = result.Elapsed.TotalSeconds
        };

        UpdateStreak(clock.Today);
        await SaveAsync();
        var currentRank = Current.RankProgress.Definition;
        var rankChanged = result.Mode == PuzzleMode.Challenge && previousRank.Tier != currentRank.Tier;
        return new ProgressUpdate(true, false, previousRank, currentRank, rankChanged,
            result.Mode == PuzzleMode.Challenge && Current.RankProgress.IsPathComplete);
    }

    private static ProgressUpdate Unchanged(RankDefinition rank) =>
        new(false, false, rank, rank, false, false);

    private void UpdateStreak(DateOnly today)
    {
        if (Current.LastCompletedDate == today)
            return;

        Current.CurrentStreak = Current.LastCompletedDate == today.AddDays(-1)
            ? Current.CurrentStreak + 1
            : 1;
        Current.BestStreak = Math.Max(Current.BestStreak, Current.CurrentStreak);
        Current.LastCompletedDate = today;
    }

    private async Task SaveAsync()
    {
        await store.SaveAsync(Current);
        ProgressChanged?.Invoke(this, EventArgs.Empty);
    }
}
