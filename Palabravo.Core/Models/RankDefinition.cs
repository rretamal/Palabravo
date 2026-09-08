namespace Palabravo.Core.Models;

public enum RankTier
{
    Novato,
    Agil,
    Ingenioso,
    Experto,
    Maestro,
    GranMaestro
}

public sealed record RankDefinition(
    RankTier Tier,
    string Name,
    int Index,
    int FirstChallenge,
    int LastChallenge,
    string AccentColor,
    string SoftColor,
    string Icon);

public sealed record RankProgressSnapshot(
    RankDefinition Definition,
    int CompletedInRank,
    int RequiredInRank,
    int TotalCompleted,
    bool IsPathComplete)
{
    public double Fraction => RequiredInRank == 0 ? 0 : CompletedInRank / (double)RequiredInRank;
}

public static class RankCatalog
{
    public const int ChallengesPerRank = 5;
    public const int TotalChallenges = 30;

    public static IReadOnlyList<RankDefinition> All { get; } =
    [
        new(RankTier.Novato, "Novato", 0, 1, 5, "#EB5B43", "#FCE4DE", "✦"),
        new(RankTier.Agil, "Ágil", 1, 6, 10, "#D5961F", "#FFF0C8", "⚡"),
        new(RankTier.Ingenioso, "Ingenioso", 2, 11, 15, "#3E9B82", "#DDEFE9", "◆"),
        new(RankTier.Experto, "Experto", 3, 16, 20, "#6477B8", "#E5E9F5", "◇"),
        new(RankTier.Maestro, "Maestro", 4, 21, 25, "#92577F", "#F2E3EE", "♜"),
        new(RankTier.GranMaestro, "Gran Maestro", 5, 26, 30, "#B98216", "#FFF2C7", "♛")
    ];

    public static RankDefinition ForChallenge(int order)
    {
        if (order is < 1 or > TotalChallenges)
            throw new ArgumentOutOfRangeException(nameof(order));

        return All[(order - 1) / ChallengesPerRank];
    }

    public static RankProgressSnapshot ForCompleted(int completedChallenges)
    {
        var completed = Math.Clamp(completedChallenges, 0, TotalChallenges);
        var rankIndex = completed >= TotalChallenges
            ? All.Count - 1
            : Math.Min(completed / ChallengesPerRank, All.Count - 1);
        var rank = All[rankIndex];
        var completedBeforeRank = rank.FirstChallenge - 1;
        var inRank = Math.Clamp(completed - completedBeforeRank, 0, ChallengesPerRank);

        return new RankProgressSnapshot(
            rank,
            inRank,
            ChallengesPerRank,
            completed,
            completed == TotalChallenges);
    }

    public static RankDefinition? Next(RankDefinition rank) =>
        rank.Index + 1 < All.Count ? All[rank.Index + 1] : null;
}
