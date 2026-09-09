using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Tests;

public sealed class PuzzleEngineTests
{
    [Fact]
    public void Two_hints_reveal_different_information()
    {
        var engine = StartedEngine();
        var first = engine.UseHint();
        var second = engine.UseHint();
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first, second);
        Assert.Equal(2, engine.State!.RevealedHints.Count);
    }

    [Fact]
    public void Correct_group_is_removed_and_recorded()
    {
        var engine = StartedEngine();
        Select(engine, "A1", "A2", "A3", "A4");

        var outcome = engine.Submit();

        Assert.Equal(SubmissionKind.Correct, outcome.Kind);
        Assert.Equal("Grupo A", outcome.SolvedGroup?.Category);
        Assert.Equal(12, engine.State?.RemainingWords.Count);
        Assert.Empty(engine.State!.SelectedWords);
    }

    [Fact]
    public void Incorrect_group_consumes_an_attempt_and_clears_selection()
    {
        var engine = StartedEngine();
        Select(engine, "A1", "B1", "C1", "D1");

        var outcome = engine.Submit();

        Assert.Equal(SubmissionKind.Incorrect, outcome.Kind);
        Assert.Equal(1, engine.State?.Errors);
        Assert.Empty(engine.State!.SelectedWords);
    }

    [Fact]
    public void Selection_never_exceeds_four_words()
    {
        var engine = StartedEngine();
        Select(engine, "A1", "A2", "B1", "B2");

        Assert.False(engine.ToggleWord("C1"));
        Assert.Equal(4, engine.State?.SelectedWords.Count);
    }

    [Fact]
    public void Third_error_ends_the_game()
    {
        var engine = StartedEngine();
        SubmissionOutcome? last = null;
        for (var i = 0; i < PuzzleEngine.MaxErrors; i++)
        {
            Select(engine, "A1", "B1", "C1", "D1");
            last = engine.Submit();
        }

        Assert.Equal(SubmissionKind.Lost, last?.Kind);
        Assert.True(engine.State?.IsFinished);
        Assert.Equal(3, engine.State?.Errors);
        Assert.Equal(Medal.None, engine.CreateResult().Medal);
    }

    [Fact]
    public void Lost_result_contains_the_complete_solution()
    {
        var engine = StartedEngine();
        for (var i = 0; i < PuzzleEngine.MaxErrors; i++)
        {
            Select(engine, "A1", "B1", "C1", "D1");
            engine.Submit();
        }

        var result = engine.CreateResult();

        Assert.False(result.IsSuccess);
        Assert.Equal(4, result.SolutionGroups.Count);
        Assert.Equal(16, result.SolutionGroups.SelectMany(group => group.Words).Count());
    }

    [Fact]
    public void Revealing_solution_finishes_without_a_medal()
    {
        var engine = StartedEngine();

        Assert.True(engine.GiveUpAndRevealSolution());
        var result = engine.CreateResult();

        Assert.True(result.SolutionRequested);
        Assert.False(result.IsSuccess);
        Assert.Equal(Medal.None, result.Medal);
        Assert.Equal(4, result.SolutionGroups.Count);
        Assert.False(engine.GiveUpAndRevealSolution());
    }

    [Fact]
    public void Hints_are_limited_to_two()
    {
        var engine = StartedEngine();

        Assert.NotNull(engine.UseHint());
        Assert.NotNull(engine.UseHint());
        Assert.Null(engine.UseHint());
        Assert.Equal(2, engine.State?.HintsUsed);
    }

    [Fact]
    public void Gold_requires_a_perfect_solution()
    {
        var engine = StartedEngine();
        foreach (var prefix in new[] { "A", "B", "C", "D" })
        {
            Select(engine, $"{prefix}1", $"{prefix}2", $"{prefix}3", $"{prefix}4");
            engine.Submit();
        }

        Assert.Equal(Medal.Gold, engine.CreateResult().Medal);
    }

    [Fact]
    public void Shuffle_keeps_exactly_the_unresolved_words()
    {
        var engine = StartedEngine();
        var before = engine.State!.RemainingWords.Order().ToArray();

        engine.Shuffle();

        Assert.Equal(before, engine.State.RemainingWords.Order().ToArray());
    }

    private static PuzzleEngine StartedEngine()
    {
        var engine = new PuzzleEngine(new Random(7));
        engine.Start(CreatePuzzle(), PuzzleMode.Challenge, new DateOnly(2026, 9, 3));
        return engine;
    }

    private static void Select(PuzzleEngine engine, params string[] words)
    {
        foreach (var word in words)
            Assert.True(engine.ToggleWord(word));
    }

    internal static PuzzleDefinition CreatePuzzle() => new()
    {
        Id = "puzzle-01",
        Order = 1,
        Title = "Prueba",
        Difficulty = "Fácil",
        Groups = Enumerable.Range(0, 4).Select(index =>
        {
            var prefix = (char)('A' + index);
            return new PuzzleGroup
            {
                Category = $"Grupo {prefix}",
                Hint = $"Pista {prefix}",
                Words = Enumerable.Range(1, 4).Select(number => $"{prefix}{number}").ToList()
            };
        }).ToList()
    };
}
