using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Tests;

public sealed class PathRankingScoreTests
{
    [Fact]
    public void Counts_only_the_best_medal_of_each_map_challenge()
    {
        var progress = new PlayerProgress();
        progress.Completions["challenge:1"] = Completion("challenge:1", PuzzleMode.Challenge, Medal.Gold);
        progress.Completions["challenge:2"] = Completion("challenge:2", PuzzleMode.Challenge, Medal.Silver);
        progress.Completions["daily:2026-09-16"] = Completion("daily:2026-09-16", PuzzleMode.Daily, Medal.Gold);
        progress.Completions["weekly:1"] = Completion("weekly:1", PuzzleMode.Weekly, Medal.Gold);

        Assert.Equal(new PathRankingScoreDetails(250, 2, 1, 1, 0), PathRankingScore.Calculate(progress));
    }

    private static CompletionRecord Completion(string key, PuzzleMode mode, Medal medal) => new()
    {
        Key = key,
        PuzzleId = key,
        Mode = mode,
        BestMedal = medal
    };
}
