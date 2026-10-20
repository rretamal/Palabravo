using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Tests;

public sealed class ContentEditionTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };
    private static T Load<T>(string file) => JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, file)), Options)!;
    private static PuzzleDefinition Copy(PuzzleDefinition puzzle) => JsonSerializer.Deserialize<PuzzleDefinition>(JsonSerializer.Serialize(puzzle), Options)!;

    [Fact]
    public void Dated_daily_calendar_is_independent_playable_and_has_complete_coverage()
    {
        var daily = Load<DailyChallengeCatalog>("daily.json");
        DailyChallengeValidator.Validate(daily);
        var first = new DateOnly(2026, 10, 21);
        Assert.Equal(14, daily.Entries.Count);
        for (var i = 0; i < 14; i++)
        {
            var date = first.AddDays(i);
            var puzzle = DailyContentSchedule.Select(daily, date)!;
            Assert.Equal($"daily-{date:yyyy-MM-dd}", puzzle.Id);
            Assert.DoesNotContain(puzzle.Id, Load<List<PuzzleDefinition>>("puzzles.json").Select(p => p.Id));
            Complete(puzzle, PuzzleMode.Daily, date);
        }
    }

    [Fact]
    public void Calendar_boundaries_do_not_reveal_future_content_and_archive_is_labeled()
    {
        var daily = Load<DailyChallengeCatalog>("daily.json");
        Assert.Null(DailyContentSchedule.Select(daily, new(2026, 10, 20)));
        var lastDate = daily.Entries.Max(e => e.Date);
        var archived = DailyContentSchedule.Select(daily, lastDate.AddDays(1))!;
        Assert.StartsWith("Del archivo · ", archived.Title);
        Assert.EndsWith("-archive", archived.Id);
        Assert.DoesNotContain(daily.Entries, e => e.Puzzle.Title.StartsWith("Del archivo"));
        Assert.Equal(archived.Id, DailyContentSchedule.Select(daily, lastDate.AddDays(1))!.Id);
    }

    [Fact]
    public void Saved_daily_selection_survives_an_offline_to_online_calendar_extension()
    {
        var old = Load<DailyChallengeCatalog>("daily.json");
        var date = old.Entries[^1].Date.AddDays(1);
        var archive = DailyContentSchedule.Select(old, date)!;
        var saved = DailyContentSchedule.SaveSelection(date, archive);
        var next = Load<DailyChallengeCatalog>("daily.json");
        var published = Copy(next.Entries[^1].Puzzle); published.Id = $"daily-{date:yyyy-MM-dd}";
        next.Entries.Add(new DailyChallengeEntry { Date = date, Puzzle = published });
        Assert.True(DailyContentSchedule.CanReplace(old, next));
        Assert.Equal(archive.Id, DailyContentSchedule.ReadSelection(saved, date)!.Id);
        Assert.Null(DailyContentSchedule.ReadSelection(saved, date.AddDays(1)));
        Assert.Null(DailyContentSchedule.ReadSelection("broken", date));
    }

    [Fact]
    public void Remote_calendar_cannot_remove_or_edit_started_competitions()
    {
        var old = Load<DailyChallengeCatalog>("daily.json");
        var next = Load<DailyChallengeCatalog>("daily.json");
        var today = old.Entries[0].Date;
        next.Entries[0].Puzzle.Questions[0].Prompt += " edited";
        Assert.False(DailyContentSchedule.CanReplace(old, next));
        next = Load<DailyChallengeCatalog>("daily.json");
        next.Entries.RemoveAt(0);
        Assert.False(DailyContentSchedule.CanReplace(old, next));
        next = Load<DailyChallengeCatalog>("daily.json");
        next.Entries[^1].Puzzle.Title += " future";
        Assert.False(DailyContentSchedule.CanReplace(old, next));
        next = Load<DailyChallengeCatalog>("daily.json");
        var addedDate = next.Entries[^1].Date.AddDays(1);
        var addedPuzzle = Copy(next.Entries[^1].Puzzle); addedPuzzle.Id = $"daily-{addedDate:yyyy-MM-dd}";
        next.Entries.Add(new DailyChallengeEntry { Date = addedDate, Puzzle = addedPuzzle });
        Assert.True(DailyContentSchedule.CanReplace(old, next));
        next.Entries.Add(next.Entries[0]);
        Assert.Throws<InvalidDataException>(() => DailyChallengeValidator.Validate(next));
    }

    [Fact]
    public void Published_active_weekly_is_unchanged_and_four_future_events_are_playable()
    {
        var all = Load<WeeklyChallengeCatalog>("weekly.json");
        WeeklyChallengeValidator.ValidateCatalog(all);
        Assert.Equal(5, all.Challenges.Count);
        var frozen = Load<WeeklyChallengeDefinition>("Fixtures/weekly-active-v1.json");
        Assert.Equal(JsonSerializer.Serialize(frozen), JsonSerializer.Serialize(all.Challenges.Single(w => w.Id == frozen.Id)));
        var ordered = all.Challenges.OrderBy(w => w.StartsAt).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var weekly = ordered[i];
            Complete(weekly.Puzzle, PuzzleMode.Weekly, DateOnly.FromDateTime(weekly.StartsAt.UtcDateTime));
            Assert.Equal(4, weekly.FinalQuestions.Count);
            Assert.All(weekly.FinalQuestions, q => Assert.Contains(q.Answer, q.Options));
            if (i > 0) Assert.Equal(ordered[i - 1].EndsAt, weekly.StartsAt);
        }
        Assert.True(WeeklyChallengeValidator.CanExtend([frozen], all.Challenges));
        Assert.False(WeeklyChallengeValidator.CanExtend(all.Challenges, [frozen]));
        var edited = Load<WeeklyChallengeCatalog>("weekly.json");
        edited.Challenges[0].Title += " changed";
        Assert.False(WeeklyChallengeValidator.CanExtend(all.Challenges, edited.Challenges));
        ordered[1].StartsAt = ordered[0].EndsAt.AddSeconds(-1);
        Assert.Throws<InvalidDataException>(() => WeeklyChallengeValidator.ValidateCatalog(all));
    }

    [Fact]
    public void New_edition_keeps_path_identity_and_bundles_the_same_catalog_and_visuals()
    {
        var legacy = Load<List<PuzzleDefinition>>("legacy-puzzles.json");
        var current = Load<List<PuzzleDefinition>>("puzzles.json");
        Assert.Equal(legacy.Select(p => (p.Id, p.Order)), current.Select(p => (p.Id, p.Order)));
        Assert.All(current, p => Assert.Equal(3, p.ContentVersion));
        Assert.All(current.SelectMany(p => p.Groups), g => Assert.False(string.IsNullOrWhiteSpace(g.Explanation)));
        var project = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "app-project.xml"));
        var assets = project.Descendants("MauiAsset").ToList();
        Assert.Contains("v2\\puzzles.json", assets.Single(a => (string?)a.Attribute("LogicalName") == "puzzles.json").Attribute("Include")!.Value);
        Assert.Contains(assets, a => (string?)a.Attribute("LogicalName") == "daily.json");
        var images = current.Concat(Load<DailyChallengeCatalog>("daily.json").Entries.Select(e => e.Puzzle))
            .SelectMany(p => p.Questions).Where(q => q.ImageSource is not null).Select(q => q.ImageSource!).Distinct().ToList();
        Assert.Equal(3, images.Count);
        Assert.All(images, file => Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, file))));
    }

    [Fact]
    public async Task Old_medals_and_unlocks_survive_the_new_edition_without_duplicate_progress()
    {
        var store = new MemoryStore();
        store.Value.Completions["challenge:puzzle-01"] = new CompletionRecord
        { Key = "challenge:puzzle-01", PuzzleId = "puzzle-01", Mode = PuzzleMode.Challenge, BestMedal = Medal.Gold };
        var service = new ProgressService(store, new Clock());
        await service.LoadAsync();
        var current = Load<List<PuzzleDefinition>>("puzzles.json");
        Assert.True(service.IsChallengeUnlocked(current[1], current));
        var result = Complete(current[0], PuzzleMode.Challenge, new(2026, 10, 21));
        var update = await service.RecordCompletionAsync(result);
        Assert.False(update.IsNewCompletion);
        Assert.Equal(1, service.Current.FixedChallengesCompleted);
        Assert.Equal(Medal.Gold, service.Current.Completions["challenge:puzzle-01"].BestMedal);
        // The medal remains attributed to the older edition when no better medal replaces it.
        Assert.Equal(0, service.Current.Completions["challenge:puzzle-01"].ContentVersion);
        await service.RecordCompletionAsync(Complete(current[1], PuzzleMode.Challenge, new(2026, 10, 21)));
        Assert.Equal(3, service.Current.Completions["challenge:puzzle-02"].ContentVersion);
    }

    [Fact]
    public async Task Improved_daily_result_keeps_the_correct_content_identity_without_duplicate_completion()
    {
        var store = new MemoryStore();
        store.Value.Completions["daily:2026-10-21"] = new CompletionRecord
        { Key = "daily:2026-10-21", PuzzleId = "puzzle-20", Mode = PuzzleMode.Daily, BestMedal = Medal.Bronze };
        var service = new ProgressService(store, new Clock());
        var puzzle = Load<DailyChallengeCatalog>("daily.json").Entries[0].Puzzle;
        await service.RecordCompletionAsync(Complete(puzzle, PuzzleMode.Daily, new(2026, 10, 21)));
        Assert.Single(service.Current.Completions);
        Assert.Equal(puzzle.Id, service.Current.Completions["daily:2026-10-21"].PuzzleId);
        Assert.Equal(puzzle.ContentVersion, service.Current.Completions["daily:2026-10-21"].ContentVersion);
    }

    [Fact]
    public void Visual_validator_rejects_missing_images_and_piece_answers_match_the_fragments()
    {
        var visual = Copy(Load<List<PuzzleDefinition>>("puzzles.json").First(p => p.Dynamic == "visual"));
        visual.Questions[0].ImageSource = null;
        Assert.Throws<InvalidDataException>(() => PuzzleCatalogValidator.ValidatePuzzle(visual));
    }

    private static GameResult Complete(PuzzleDefinition puzzle, PuzzleMode mode, DateOnly date)
    {
        var engine = new PuzzleEngine(); engine.Start(puzzle, mode, date);
        foreach (var group in puzzle.Groups)
        {
            foreach (var word in group.Words) Assert.True(engine.ToggleWord(word));
            engine.Submit();
        }
        foreach (var question in puzzle.Questions) engine.AnswerQuestion(question.Answer);
        var result = engine.CreateResult();
        Assert.True(result.IsSuccess); Assert.Equal(0, result.Errors);
        Assert.Equal(puzzle.ContentVersion, result.ContentVersion);
        return result;
    }
    private sealed class Clock : IClock { public DateOnly Today => new(2026, 10, 21); public DateOnly UtcToday => Today; }
    private sealed class MemoryStore : IProgressStore
    {
        public PlayerProgress Value { get; } = new();
        public Task<PlayerProgress> LoadAsync() => Task.FromResult(Value);
        public Task SaveAsync(PlayerProgress progress) => Task.CompletedTask;
    }
}
