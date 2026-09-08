using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Tests;

public sealed class LeaderboardScoreTests
{
    [Fact]
    public void Medal_has_priority_over_errors_hints_and_time()
    {
        var slowSilver = Result(Medal.Silver, 1, 1, TimeSpan.FromHours(2));
        var fastBronze = Result(Medal.Bronze, 0, 2, TimeSpan.FromSeconds(1));

        Assert.True(LeaderboardScore.Encode(slowSilver) > LeaderboardScore.Encode(fastBronze));
    }

    [Fact]
    public void Fewer_errors_hints_and_seconds_rank_higher()
    {
        var best = Result(Medal.Bronze, 1, 1, TimeSpan.FromSeconds(30));
        var error = Result(Medal.Bronze, 2, 0, TimeSpan.FromSeconds(1));
        var hint = Result(Medal.Bronze, 1, 2, TimeSpan.FromSeconds(1));
        var time = Result(Medal.Bronze, 1, 1, TimeSpan.FromSeconds(31));

        Assert.True(LeaderboardScore.Encode(best) > LeaderboardScore.Encode(error));
        Assert.True(LeaderboardScore.Encode(best) > LeaderboardScore.Encode(hint));
        Assert.True(LeaderboardScore.Encode(best) > LeaderboardScore.Encode(time));
    }

    [Fact]
    public void Score_round_trips_visible_statistics()
    {
        var result = Result(Medal.Silver, 1, 1, TimeSpan.FromSeconds(93));

        var details = LeaderboardScore.Decode(LeaderboardScore.Encode(result));

        Assert.Equal(result.Medal, details.Medal);
        Assert.Equal(result.Errors, details.Errors);
        Assert.Equal(result.HintsUsed, details.Hints);
        Assert.Equal(result.Elapsed, details.Elapsed);
    }

    [Fact]
    public void Failed_game_cannot_be_ranked()
    {
        var result = new GameResult("puzzle-01", "Reto", PuzzleMode.Daily, new DateOnly(2026, 9, 4),
            false, Medal.None, PuzzleEngine.MaxErrors, 2, false, TimeSpan.FromMinutes(1), [], []);

        Assert.Throws<ArgumentException>(() => LeaderboardScore.Encode(result));
    }

    private static GameResult Result(Medal medal, int errors, int hints, TimeSpan elapsed) =>
        new("puzzle-01", "Reto", PuzzleMode.Daily, new DateOnly(2026, 9, 4),
            true, medal, errors, hints, false, elapsed, [], []);
}
