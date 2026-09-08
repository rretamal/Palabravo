using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Tests;

public sealed class ProgressServiceTests
{
    [Fact]
    public async Task Replay_is_idempotent_but_keeps_the_better_medal()
    {
        var store = new MemoryStore();
        var service = new ProgressService(store, new FixedClock(new DateOnly(2026, 9, 3)));
        await service.RecordCompletionAsync(Result("puzzle-01", PuzzleMode.Challenge, Medal.Bronze, 3, 2));
        await service.RecordCompletionAsync(Result("puzzle-01", PuzzleMode.Challenge, Medal.Gold, 0, 0));

        Assert.Single(service.Current.Completions);
        Assert.Equal(Medal.Gold, service.Current.Completions["challenge:puzzle-01"].BestMedal);
        Assert.Equal(1, service.Current.TotalResolved);
    }

    [Fact]
    public async Task Daily_counts_once_per_date()
    {
        var date = new DateOnly(2026, 9, 3);
        var service = new ProgressService(new MemoryStore(), new FixedClock(date));
        await service.RecordCompletionAsync(Result("puzzle-01", PuzzleMode.Daily, Medal.Silver, 1, 0, date));
        await service.RecordCompletionAsync(Result("puzzle-02", PuzzleMode.Daily, Medal.Bronze, 2, 1, date));

        Assert.Single(service.Current.Completions);
    }

    [Fact]
    public async Task Five_fixed_challenges_unlock_agile_rank()
    {
        var service = new ProgressService(new MemoryStore(), new FixedClock(new DateOnly(2026, 9, 3)));
        for (var i = 1; i <= 5; i++)
            await service.RecordCompletionAsync(Result($"puzzle-{i:00}", PuzzleMode.Challenge, Medal.Gold, 0, 0));

        Assert.Equal("Ágil", service.Current.Rank);
        Assert.Equal(5, service.Current.FixedChallengesCompleted);
        Assert.Equal(0, service.Current.RankProgress.CompletedInRank);
    }

    [Theory]
    [InlineData(0, RankTier.Novato, 0, false)]
    [InlineData(4, RankTier.Novato, 4, false)]
    [InlineData(5, RankTier.Agil, 0, false)]
    [InlineData(9, RankTier.Agil, 4, false)]
    [InlineData(10, RankTier.Ingenioso, 0, false)]
    [InlineData(14, RankTier.Ingenioso, 4, false)]
    [InlineData(15, RankTier.Experto, 0, false)]
    [InlineData(19, RankTier.Experto, 4, false)]
    [InlineData(20, RankTier.Maestro, 0, false)]
    [InlineData(24, RankTier.Maestro, 4, false)]
    [InlineData(25, RankTier.GranMaestro, 0, false)]
    [InlineData(30, RankTier.GranMaestro, 5, true)]
    public void Rank_boundaries_are_calculated_from_challenge_completions(
        int completed, RankTier expectedTier, int expectedInRank, bool pathComplete)
    {
        var progress = ProgressWithChallenges(completed);

        Assert.Equal(expectedTier, progress.RankTier);
        Assert.Equal(expectedInRank, progress.RankProgress.CompletedInRank);
        Assert.Equal(pathComplete, progress.RankProgress.IsPathComplete);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    [InlineData(25)]
    [InlineData(30)]
    public async Task Completing_a_challenge_unlocks_exactly_the_next_one(int completedOrder)
    {
        var service = new ProgressService(new MemoryStore(), new FixedClock(new DateOnly(2026, 9, 3)));
        var catalog = FakeCatalog();
        for (var order = 1; order <= completedOrder; order++)
            await service.RecordCompletionAsync(Result($"puzzle-{order:00}", PuzzleMode.Challenge, Medal.Bronze, 0, 0));

        Assert.All(catalog.Take(completedOrder), puzzle => Assert.True(service.IsChallengeUnlocked(puzzle, catalog)));
        if (completedOrder < RankCatalog.TotalChallenges)
        {
            Assert.True(service.IsChallengeUnlocked(catalog[completedOrder], catalog));
            if (completedOrder + 1 < catalog.Count)
                Assert.False(service.IsChallengeUnlocked(catalog[completedOrder + 1], catalog));
        }
    }

    [Theory]
    [InlineData(5, RankTier.Novato, RankTier.Agil)]
    [InlineData(10, RankTier.Agil, RankTier.Ingenioso)]
    [InlineData(15, RankTier.Ingenioso, RankTier.Experto)]
    [InlineData(20, RankTier.Experto, RankTier.Maestro)]
    [InlineData(25, RankTier.Maestro, RankTier.GranMaestro)]
    public async Task Rank_up_is_emitted_only_once_at_every_boundary(
        int boundary, RankTier previousTier, RankTier currentTier)
    {
        var service = new ProgressService(new MemoryStore(), new FixedClock(new DateOnly(2026, 9, 3)));
        for (var order = 1; order < boundary; order++)
            await service.RecordCompletionAsync(Result($"puzzle-{order:00}", PuzzleMode.Challenge, Medal.Bronze, 0, 0));

        var id = $"puzzle-{boundary:00}";
        var promotion = await service.RecordCompletionAsync(Result(id, PuzzleMode.Challenge, Medal.Bronze, 0, 0));
        var replay = await service.RecordCompletionAsync(Result(id, PuzzleMode.Challenge, Medal.Gold, 0, 0));

        Assert.True(promotion.RankChanged);
        Assert.Equal(previousTier, promotion.PreviousRank.Tier);
        Assert.Equal(currentTier, promotion.CurrentRank.Tier);
        Assert.False(replay.RankChanged);
        Assert.True(replay.MedalImproved);
    }

    [Fact]
    public async Task First_completion_of_challenge_thirty_completes_the_path_once()
    {
        var service = new ProgressService(new MemoryStore(), new FixedClock(new DateOnly(2026, 9, 3)));
        for (var order = 1; order < RankCatalog.TotalChallenges; order++)
            await service.RecordCompletionAsync(Result($"puzzle-{order:00}", PuzzleMode.Challenge, Medal.Bronze, 0, 0));

        var completion = await service.RecordCompletionAsync(Result("puzzle-30", PuzzleMode.Challenge, Medal.Bronze, 0, 0));
        var replay = await service.RecordCompletionAsync(Result("puzzle-30", PuzzleMode.Challenge, Medal.Gold, 0, 0));

        Assert.True(completion.PathCompleted);
        Assert.False(replay.PathCompleted);
        Assert.True(service.Current.RankProgress.IsPathComplete);
    }

    [Fact]
    public async Task Daily_completion_does_not_advance_or_unlock_the_path()
    {
        var service = new ProgressService(new MemoryStore(), new FixedClock(new DateOnly(2026, 9, 3)));
        var catalog = FakeCatalog();

        var update = await service.RecordCompletionAsync(Result("puzzle-18", PuzzleMode.Daily, Medal.Gold, 0, 0));

        Assert.Equal(0, service.Current.FixedChallengesCompleted);
        Assert.Equal(RankTier.Novato, service.Current.RankTier);
        Assert.False(update.RankChanged);
        Assert.False(service.IsChallengeUnlocked(catalog[1], catalog));
    }

    [Fact]
    public async Task Consecutive_dates_increment_streak()
    {
        var clock = new FixedClock(new DateOnly(2026, 9, 3));
        var service = new ProgressService(new MemoryStore(), clock);
        await service.RecordCompletionAsync(Result("puzzle-01", PuzzleMode.Challenge, Medal.Gold, 0, 0));
        clock.Today = clock.Today.AddDays(1);
        await service.RecordCompletionAsync(Result("puzzle-01", PuzzleMode.Daily, Medal.Gold, 0, 0, clock.Today));

        Assert.Equal(2, service.Current.CurrentStreak);
        Assert.Equal(2, service.Current.BestStreak);
    }

    [Fact]
    public async Task Reset_clears_progress_streaks_and_tutorial_state()
    {
        var store = new MemoryStore();
        var service = new ProgressService(store, new FixedClock(new DateOnly(2026, 9, 3)));
        await service.MarkTutorialSeenAsync();
        await service.RecordCompletionAsync(Result("puzzle-01", PuzzleMode.Challenge, Medal.Gold, 0, 0));

        await service.ResetAsync();

        Assert.Empty(service.Current.Completions);
        Assert.False(service.Current.TutorialSeen);
        Assert.Equal(0, service.Current.CurrentStreak);
        Assert.Equal(0, service.Current.BestStreak);
        Assert.Equal(PlayerProgress.CurrentSchemaVersion, store.Value.SchemaVersion);
    }

    [Fact]
    public void Invalid_json_returns_safe_defaults()
    {
        var progress = ProgressJson.DeserializeSafe("{ definitely invalid }");
        Assert.Equal(PlayerProgress.CurrentSchemaVersion, progress.SchemaVersion);
        Assert.Empty(progress.Completions);
    }

    [Fact]
    public void Version_one_migration_resets_gameplay_and_preserves_tutorial()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "tutorialSeen": true,
              "currentStreak": 9,
              "bestStreak": 12,
              "lastCompletedDate": "2026-09-03",
              "completions": {
                "challenge:puzzle-01": {
                  "key": "challenge:puzzle-01",
                  "puzzleId": "puzzle-01",
                  "mode": 0,
                  "completedOn": "2026-09-03",
                  "bestMedal": 3
                }
              }
            }
            """;

        var progress = ProgressJson.DeserializeSafe(json, out var migrated);

        Assert.True(migrated);
        Assert.Equal(PlayerProgress.CurrentSchemaVersion, progress.SchemaVersion);
        Assert.True(progress.TutorialSeen);
        Assert.Empty(progress.Completions);
        Assert.Equal(0, progress.CurrentStreak);
        Assert.Equal(0, progress.BestStreak);
        Assert.Null(progress.LastCompletedDate);
    }

    private static GameResult Result(string id, PuzzleMode mode, Medal medal, int errors, int hints, DateOnly? date = null) =>
        new(id, "Reto", mode, date ?? new DateOnly(2026, 9, 3), true, medal, errors, hints, false, TimeSpan.FromMinutes(2), [], []);

    private static PlayerProgress ProgressWithChallenges(int completed)
    {
        var progress = new PlayerProgress();
        for (var order = 1; order <= completed; order++)
        {
            var id = $"puzzle-{order:00}";
            progress.Completions[$"challenge:{id}"] = new CompletionRecord
            {
                Key = $"challenge:{id}",
                PuzzleId = id,
                Mode = PuzzleMode.Challenge
            };
        }

        return progress;
    }

    private static IReadOnlyList<PuzzleDefinition> FakeCatalog() =>
        Enumerable.Range(1, RankCatalog.TotalChallenges).Select(order => new PuzzleDefinition
        {
            Id = $"puzzle-{order:00}",
            Order = order,
            Rank = RankCatalog.ForChallenge(order).Tier,
            Title = $"Reto {order}"
        }).ToList();

    private sealed class MemoryStore : IProgressStore
    {
        public PlayerProgress Value { get; private set; } = new();
        public Task<PlayerProgress> LoadAsync() => Task.FromResult(Value);
        public Task SaveAsync(PlayerProgress progress) { Value = progress; return Task.CompletedTask; }
    }

    private sealed class FixedClock(DateOnly today) : IClock
    {
        public DateOnly Today { get; set; } = today;
        public DateOnly UtcToday => Today;
    }
}
