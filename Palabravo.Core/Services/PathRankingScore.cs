using Palabravo.Core.Models;

namespace Palabravo.Core.Services;

public static class PathRankingScore
{
    public const int CompletionPoints = 100;

    public static PathRankingScoreDetails Calculate(PlayerProgress progress)
    {
        var completions = progress.Completions.Values
            .Where(item => item.Mode == PuzzleMode.Challenge)
            .ToList();
        var gold = completions.Count(item => item.BestMedal == Medal.Gold);
        var silver = completions.Count(item => item.BestMedal == Medal.Silver);
        var bronze = completions.Count(item => item.BestMedal == Medal.Bronze);
        var score = completions.Count * CompletionPoints + gold * 30 + silver * 20 + bronze * 10;
        return new(score, completions.Count, gold, silver, bronze);
    }
}

public sealed record PathRankingScoreDetails(int Score, int Completed, int Gold, int Silver, int Bronze);
