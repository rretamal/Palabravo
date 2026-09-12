using System.Text.Json;
using System.Text.Json.Serialization;
using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Tests;

public sealed class PuzzleCatalogTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public void Production_catalog_has_the_complete_valid_structure()
    {
        var puzzles = LoadCatalog();

        PuzzleCatalogValidator.Validate(puzzles);
        Assert.Equal(30, puzzles.Count);
        Assert.Equal(10, puzzles.Count(puzzle => puzzle.Dynamic == "connections"));
        Assert.Equal(7, puzzles.Select(puzzle => puzzle.Dynamic).Distinct().Count());
        Assert.Equal(30, puzzles.Select(puzzle => puzzle.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(30, puzzles.Select(puzzle => puzzle.Order).Distinct().Count());
        Assert.All(RankCatalog.All, rank =>
            Assert.Equal(5, puzzles.Count(puzzle => puzzle.Rank == rank.Tier)));
        Assert.All(puzzles, puzzle =>
        {
            Assert.Equal($"puzzle-{puzzle.Order:00}", puzzle.Id);
            Assert.True(puzzle.ContentVersion > 0);
            Assert.False(string.IsNullOrWhiteSpace(puzzle.Title));
            Assert.All(puzzle.Groups, group => Assert.False(string.IsNullOrWhiteSpace(group.Hint)));
        });
    }

    [Fact]
    public void Every_catalog_puzzle_can_be_completed_from_its_declared_groups()
    {
        foreach (var puzzle in LoadCatalog())
        {
            var engine = new PuzzleEngine(new Random(puzzle.Order));
            engine.Start(puzzle, PuzzleMode.Challenge, new DateOnly(2026, 9, 7));

            SubmissionOutcome? outcome = null;
            foreach (var group in puzzle.Groups)
            {
                foreach (var word in group.Words)
                    Assert.True(engine.ToggleWord(word));
                outcome = engine.Submit();
            }
            foreach (var question in puzzle.Questions)
                outcome = engine.AnswerQuestion(question.Answer);

            Assert.Equal(SubmissionKind.Won, outcome?.Kind);
            Assert.True(engine.State?.IsFinished);
        }
    }

    [Fact]
    public void Appended_challenge_is_valid_and_wrong_answers_cannot_complete_it()
    {
        var puzzles = LoadCatalog();
        var extra = new PuzzleDefinition { Id = "puzzle-31", Order = 31, Rank = RankTier.GranMaestro,
            Title = "Nuevo reto", Difficulty = "Difícil", Dynamic = "trivia", Questions = puzzles.First(p => p.Dynamic == "trivia").Questions };
        puzzles.Add(extra);
        PuzzleCatalogValidator.Validate(puzzles);
        var engine = new PuzzleEngine();
        engine.Start(extra, PuzzleMode.Challenge, new DateOnly(2026, 9, 12));
        Assert.Throws<InvalidOperationException>(() => engine.CreateResult());
        for (var i = 0; i < 3; i++) engine.AnswerQuestion(extra.Questions[0].Options.First(o => o != extra.Questions[0].Answer));
        Assert.False(engine.CreateResult().IsSuccess);
        Assert.Equal(Medal.None, engine.CreateResult().Medal);
        Assert.Equal(SubmissionKind.NotReady, engine.AnswerQuestion(extra.Questions[0].Answer).Kind);
    }

    [Fact]
    public void Question_hints_are_partial_and_do_not_repeat()
    {
        var puzzle = LoadCatalog().First(p => p.Dynamic == "trivia");
        var engine = new PuzzleEngine();
        engine.Start(puzzle, PuzzleMode.Challenge, new DateOnly(2026, 9, 12));
        Assert.Equal(puzzle.Questions[0].Hint, engine.UseHint());
        Assert.Null(engine.UseHint());
        engine.AnswerQuestion(puzzle.Questions[0].Answer);
        Assert.Equal(puzzle.Questions[1].Hint, engine.UseHint());
        Assert.Equal(2, engine.State!.HintsUsed);
    }

    [Fact]
    public void Validator_rejects_a_duplicate_word()
    {
        var puzzles = LoadCatalog();
        puzzles[0].Groups[1].Words[0] = puzzles[0].Groups[0].Words[0];

        Assert.Throws<InvalidDataException>(() => PuzzleCatalogValidator.Validate(puzzles));
    }

    [Fact]
    public void Validator_rejects_a_duplicate_id_and_order()
    {
        var puzzles = LoadCatalog();
        puzzles[1].Id = puzzles[0].Id;
        puzzles[1].Order = puzzles[0].Order;

        Assert.Throws<InvalidDataException>(() => PuzzleCatalogValidator.Validate(puzzles));
    }

    [Fact]
    public void Daily_schedule_is_stable_and_cycles_through_all_thirty_puzzles()
    {
        var puzzles = LoadCatalog().OrderBy(puzzle => puzzle.Order).ToList();
        var start = new DateOnly(2026, 1, 1);

        Assert.Same(DailyPuzzleSchedule.Select(puzzles, start), DailyPuzzleSchedule.Select(puzzles, start));
        Assert.Equal(30, Enumerable.Range(0, 30)
            .Select(offset => DailyPuzzleSchedule.Select(puzzles, start.AddDays(offset)).Id)
            .Distinct()
            .Count());
        Assert.Equal(
            DailyPuzzleSchedule.Select(puzzles, start).Id,
            DailyPuzzleSchedule.Select(puzzles, start.AddDays(30)).Id);
    }

    [Theory]
    [InlineData(8, " COMIBLE ", "BEBESTIBLE", "OI\u0301BLE", "tocable")]
    [InlineData(22, "observable", "LEIBLE", "comprensible", "CREIBLE")]
    public void Written_challenges_accept_variants_without_penalty(int order, params string[] answers)
    {
        var puzzle = LoadCatalog().Single(p => p.Order == order);
        Assert.Equal("recall", puzzle.Dynamic);
        var engine = new PuzzleEngine();
        engine.Start(puzzle, PuzzleMode.Challenge, new DateOnly(2026, 9, 12));
        foreach (var answer in answers) engine.AnswerQuestion(answer);
        Assert.True(engine.CreateResult().IsSuccess);
        Assert.Equal(0, engine.State!.Errors);
        Assert.Equal(Medal.Gold, engine.CreateResult().Medal);
        Assert.NotEqual(PathQuestion.NormalizeAnswer("año"), PathQuestion.NormalizeAnswer("ano"));
    }

    [Fact]
    public void Written_answers_ignore_empty_input_and_lose_after_three_errors()
    {
        var engine = new PuzzleEngine();
        engine.Start(LoadCatalog().Single(p => p.Order == 8), PuzzleMode.Challenge, new DateOnly(2026, 9, 12));
        Assert.Equal(SubmissionKind.NotReady, engine.AnswerQuestion("  ").Kind);
        Assert.Equal(SubmissionKind.NotReady, engine.AnswerQuestion(new string('a', 81)).Kind);
        Assert.Equal(0, engine.State!.Errors);
        Assert.Equal(SubmissionKind.Incorrect, engine.AnswerQuestion("inventado").Kind);
        Assert.Equal(0, engine.State.QuestionIndex);
        Assert.Equal(SubmissionKind.Incorrect, engine.AnswerQuestion("inventado").Kind);
        Assert.Equal(SubmissionKind.Lost, engine.AnswerQuestion("inventado").Kind);
        Assert.False(engine.CreateResult().IsSuccess);
        Assert.Equal(SubmissionKind.NotReady, engine.AnswerQuestion("comestible").Kind);
    }

    private static List<PuzzleDefinition> LoadCatalog()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "puzzles.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<PuzzleDefinition>>(json, JsonOptions)
            ?? throw new InvalidDataException("No fue posible leer el catálogo de pruebas.");
    }
}
