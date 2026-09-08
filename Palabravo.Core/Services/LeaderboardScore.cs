using Palabravo.Core.Models;

namespace Palabravo.Core.Services;

public static class LeaderboardScore
{
    private const long MedalFactor = 10_000_000;
    private const long ErrorFactor = 1_000_000;
    private const long HintFactor = 100_000;
    private const int TimeCeilingSeconds = 99_999;

    public static long Encode(GameResult result)
    {
        if (!result.IsSuccess || result.Medal == Medal.None)
            throw new ArgumentException("Solo las partidas resueltas pueden entrar al ranking.", nameof(result));

        var seconds = Math.Clamp((int)Math.Round(result.Elapsed.TotalSeconds), 0, TimeCeilingSeconds);
        return (long)result.Medal * MedalFactor
            + (4 - Math.Clamp(result.Errors, 0, 4)) * ErrorFactor
            + (2 - Math.Clamp(result.HintsUsed, 0, 2)) * HintFactor
            + (TimeCeilingSeconds - seconds);
    }

    public static LeaderboardScoreDetails Decode(long score)
    {
        var medalValue = Math.Clamp((int)(score / MedalFactor), (int)Medal.None, (int)Medal.Gold);
        var remainder = Math.Max(0, score % MedalFactor);
        var errors = 4 - Math.Clamp((int)(remainder / ErrorFactor), 0, 4);
        remainder %= ErrorFactor;
        var hints = 2 - Math.Clamp((int)(remainder / HintFactor), 0, 2);
        var seconds = TimeCeilingSeconds - (int)(remainder % HintFactor);
        return new LeaderboardScoreDetails((Medal)medalValue, errors, hints, TimeSpan.FromSeconds(seconds));
    }
}

public sealed record LeaderboardScoreDetails(Medal Medal, int Errors, int Hints, TimeSpan Elapsed);
