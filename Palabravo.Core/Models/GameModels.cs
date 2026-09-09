namespace Palabravo.Core.Models;

public enum Medal
{
    None = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3
}

public enum PuzzleMode
{
    Challenge,
    Daily,
    Weekly
}

public enum SubmissionKind
{
    NotReady,
    Incorrect,
    Correct,
    Won,
    Lost
}

public sealed record SubmissionOutcome(SubmissionKind Kind, PuzzleGroup? SolvedGroup = null);

public sealed record GameResult(
    string PuzzleId,
    string PuzzleTitle,
    PuzzleMode Mode,
    DateOnly PlayedOn,
    bool IsSuccess,
    Medal Medal,
    int Errors,
    int HintsUsed,
    bool SolutionRequested,
    TimeSpan Elapsed,
    IReadOnlyList<PuzzleGroup> SolvedGroups,
    IReadOnlyList<PuzzleGroup> SolutionGroups);

public sealed class GameState
{
    public string AttemptId { get; init; } = Guid.NewGuid().ToString("N");
    public required PuzzleDefinition Puzzle { get; init; }
    public required PuzzleMode Mode { get; init; }
    public required DateOnly PlayedOn { get; init; }
    public List<string> RemainingWords { get; } = [];
    public HashSet<string> SelectedWords { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<PuzzleGroup> SolvedGroups { get; } = [];
    public HashSet<string> RevealedHints { get; } = [];
    public int Errors { get; internal set; }
    public int HintsUsed { get; internal set; }
    public bool SolutionRequested { get; internal set; }
    public bool IsFinished { get; internal set; }
}
