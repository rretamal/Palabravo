namespace Palabravo.Core.Models;

public sealed class PlayerProgress
{
    public const int CurrentSchemaVersion = 3;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public bool TutorialSeen { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public DateOnly? LastCompletedDate { get; set; }
    public Dictionary<string, CompletionRecord> Completions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<SpecialBadgeRecord> SpecialBadges { get; set; } = [];

    public int TotalResolved => Completions.Count;
    public int PerfectResolved => Completions.Values.Count(x => x.Errors == 0 && x.HintsUsed == 0);
    public int PerfectPercentage => TotalResolved == 0 ? 0 : (int)Math.Round(PerfectResolved * 100d / TotalResolved);
    public int FixedChallengesCompleted => Completions.Values.Count(x => x.Mode == PuzzleMode.Challenge);
    public RankProgressSnapshot RankProgress => RankCatalog.ForCompleted(FixedChallengesCompleted);
    public RankTier RankTier => RankProgress.Definition.Tier;
    public string Rank => RankProgress.Definition.Name;
}

public sealed class SpecialBadgeRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "✦";
    public DateTimeOffset EarnedAt { get; set; }
}

public sealed class CompletionRecord
{
    public string Key { get; set; } = string.Empty;
    public string PuzzleId { get; set; } = string.Empty;
    public PuzzleMode Mode { get; set; }
    public DateOnly CompletedOn { get; set; }
    public Medal BestMedal { get; set; }
    public int Errors { get; set; }
    public int HintsUsed { get; set; }
    public double ElapsedSeconds { get; set; }
}
