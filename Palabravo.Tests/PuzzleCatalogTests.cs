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
        Assert.Equal(120, puzzles.Sum(puzzle => puzzle.Groups.Count));
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

            Assert.Equal(SubmissionKind.Won, outcome?.Kind);
            Assert.True(engine.State?.IsFinished);
        }
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

    private static List<PuzzleDefinition> LoadCatalog()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "puzzles.json");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<PuzzleDefinition>>(json, JsonOptions)
            ?? throw new InvalidDataException("No fue posible leer el catálogo de pruebas.");
    }
}
