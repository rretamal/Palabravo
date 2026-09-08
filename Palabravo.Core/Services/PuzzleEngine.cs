using System.Diagnostics;
using Palabravo.Core.Models;

namespace Palabravo.Core.Services;

public sealed class PuzzleEngine
{
    public const int MaxErrors = 3;

    private readonly Random _random;
    private readonly Stopwatch _stopwatch = new();

    public PuzzleEngine(Random? random = null) => _random = random ?? Random.Shared;

    public GameState? State { get; private set; }
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public GameState Start(PuzzleDefinition puzzle, PuzzleMode mode, DateOnly playedOn)
    {
        ValidatePuzzle(puzzle);
        _stopwatch.Restart();
        State = new GameState { Puzzle = puzzle, Mode = mode, PlayedOn = playedOn };
        State.RemainingWords.AddRange(puzzle.Groups.SelectMany(x => x.Words));
        Shuffle();
        return State;
    }

    public bool ToggleWord(string word)
    {
        var state = RequireState();
        if (state.IsFinished || !state.RemainingWords.Contains(word, StringComparer.OrdinalIgnoreCase))
            return false;

        if (state.SelectedWords.Remove(word))
            return true;

        if (state.SelectedWords.Count >= 4)
            return false;

        return state.SelectedWords.Add(word);
    }

    public SubmissionOutcome Submit()
    {
        var state = RequireState();
        if (state.IsFinished || state.SelectedWords.Count != 4)
            return new SubmissionOutcome(SubmissionKind.NotReady);

        var match = state.Puzzle.Groups.FirstOrDefault(group =>
            group.Words.Count == state.SelectedWords.Count &&
            group.Words.All(state.SelectedWords.Contains));

        if (match is null)
        {
            state.Errors++;
            state.SelectedWords.Clear();
            if (state.Errors < MaxErrors)
                return new SubmissionOutcome(SubmissionKind.Incorrect);

            state.IsFinished = true;
            _stopwatch.Stop();
            return new SubmissionOutcome(SubmissionKind.Lost);
        }

        state.SolvedGroups.Add(match);
        state.RemainingWords.RemoveAll(word => match.Words.Contains(word, StringComparer.OrdinalIgnoreCase));
        state.SelectedWords.Clear();
        if (state.SolvedGroups.Count < 4)
            return new SubmissionOutcome(SubmissionKind.Correct, match);

        state.IsFinished = true;
        _stopwatch.Stop();
        return new SubmissionOutcome(SubmissionKind.Won, match);
    }

    public string? UseHint()
    {
        var state = RequireState();
        if (state.IsFinished || state.HintsUsed >= 2)
            return null;

        var group = state.Puzzle.Groups.FirstOrDefault(x => !state.SolvedGroups.Contains(x));
        if (group is null)
            return null;

        state.HintsUsed++;
        return group.Hint;
    }

    public bool GiveUpAndRevealSolution()
    {
        var state = RequireState();
        if (state.IsFinished)
            return false;

        state.SolutionRequested = true;
        state.IsFinished = true;
        state.SelectedWords.Clear();
        _stopwatch.Stop();
        return true;
    }

    public void Shuffle()
    {
        var state = RequireState();
        for (var i = state.RemainingWords.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (state.RemainingWords[i], state.RemainingWords[j]) = (state.RemainingWords[j], state.RemainingWords[i]);
        }
    }

    public GameResult CreateResult()
    {
        var state = RequireState();
        if (!state.IsFinished)
            throw new InvalidOperationException("La partida todavía no termina.");

        var succeeded = state.SolvedGroups.Count == 4;
        var medal = !succeeded ? Medal.None : state.Errors == 0 && state.HintsUsed == 0
            ? Medal.Gold
            : state.Errors <= 1 && state.HintsUsed <= 1 ? Medal.Silver : Medal.Bronze;

        return new GameResult(state.Puzzle.Id, state.Puzzle.Title, state.Mode, state.PlayedOn,
            succeeded, medal, state.Errors, state.HintsUsed, state.SolutionRequested, Elapsed,
            state.SolvedGroups.ToList(), state.Puzzle.Groups.ToList());
    }

    private GameState RequireState() => State ?? throw new InvalidOperationException("No hay una partida activa.");

    private static void ValidatePuzzle(PuzzleDefinition puzzle)
    {
        if (puzzle.Groups.Count != 4 || puzzle.Groups.Any(x => x.Words.Count != 4))
            throw new ArgumentException("Cada puzzle debe contener cuatro grupos de cuatro palabras.", nameof(puzzle));

        var words = puzzle.Groups.SelectMany(x => x.Words).ToList();
        if (words.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 16)
            throw new ArgumentException("Las dieciséis palabras deben ser únicas.", nameof(puzzle));
    }
}
