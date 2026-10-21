using Palabravo.Core.Models;

namespace Palabravo.Core.Services;

public static class RankingParticipation
{
    // Recorded completions represent successful games, including older editions.
    public static bool CanViewPath(PlayerProgress progress) => progress.TotalResolved > 0;

    public static bool CanViewWeekly(PlayerProgress progress, string activePuzzleId) =>
        progress.HasCompletedWeekly(activePuzzleId);
}
