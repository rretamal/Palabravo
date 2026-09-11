using System.Text.Json;
using System.Text.Json.Serialization;
using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Tests;

public sealed class WeeklyChallengeTests
{
    [Fact]
    public void Published_weekly_is_valid_and_active_during_its_window()
    {
        var weekly = Load();
        WeeklyChallengeValidator.Validate(weekly);

        Assert.True(weekly.IsActive(DateTimeOffset.Parse("2026-09-09T12:00:00Z")));
        Assert.False(weekly.IsActive(weekly.EndsAt));
        Assert.Equal(16, weekly.Puzzle.Groups.SelectMany(group => group.Words).Count());
        Assert.Equal(4, weekly.FinalQuestions.Count);
        Assert.All(weekly.FinalQuestions, question =>
            Assert.Contains(question.Answer, question.Options, StringComparer.OrdinalIgnoreCase));
        Assert.Single(weekly.FinalQuestions, question => !string.IsNullOrWhiteSpace(question.ImageSource));
    }

    [Fact]
    public async Task Weekly_completion_earns_one_badge_without_advancing_the_path()
    {
        var weekly = Load();
        var store = new MemoryStore();
        var service = new ProgressService(store, new FixedClock());
        var result = new GameResult(weekly.Puzzle.Id, weekly.Title, PuzzleMode.Weekly,
            new DateOnly(2026, 9, 9), true, Medal.Bronze, 2, 1, false,
            TimeSpan.FromMinutes(3), [], weekly.Puzzle.Groups);

        await service.RecordCompletionAsync(result);
        Assert.True(await service.EarnSpecialBadgeAsync(weekly.Badge));
        Assert.False(await service.EarnSpecialBadgeAsync(weekly.Badge));

        Assert.Equal(0, service.Current.FixedChallengesCompleted);
        Assert.Single(service.Current.SpecialBadges);
        Assert.Contains($"weekly:{weekly.Puzzle.Id}", service.Current.Completions.Keys);
        Assert.True(service.Current.HasCompletedWeekly(weekly.Puzzle.Id));
    }

    [Fact]
    public void Version_two_progress_keeps_completions_when_badges_are_added()
    {
        const string json = """
            {"schemaVersion":2,"tutorialSeen":true,"completions":{"challenge:puzzle-01":{"key":"challenge:puzzle-01","puzzleId":"puzzle-01","mode":0}}}
            """;

        var progress = ProgressJson.DeserializeSafe(json, out var migrated);

        Assert.True(migrated);
        Assert.True(progress.TutorialSeen);
        Assert.Single(progress.Completions);
        Assert.Empty(progress.SpecialBadges);
        Assert.Equal(PlayerProgress.CurrentSchemaVersion, progress.SchemaVersion);
    }

    private static WeeklyChallengeDefinition Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "weekly.json");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return JsonSerializer.Deserialize<WeeklyChallengeCatalog>(File.ReadAllText(path), options)!.Challenges.Single();
    }

    private sealed class MemoryStore : IProgressStore
    {
        public PlayerProgress Value { get; private set; } = new();
        public Task<PlayerProgress> LoadAsync() => Task.FromResult(Value);
        public Task SaveAsync(PlayerProgress progress) { Value = progress; return Task.CompletedTask; }
    }
    private sealed class FixedClock : IClock
    {
        public DateOnly Today => new(2026, 9, 9);
        public DateOnly UtcToday => Today;
    }
}
