using Palabravo.Core.Models;

namespace Palabravo.Core.Services;

public static class DailyPuzzleSchedule
{
    public static PuzzleDefinition Select(IReadOnlyList<PuzzleDefinition> orderedPuzzles, DateOnly utcDate)
    {
        ArgumentNullException.ThrowIfNull(orderedPuzzles);
        if (orderedPuzzles.Count == 0)
            throw new ArgumentException("El catálogo no contiene retos.", nameof(orderedPuzzles));

        return orderedPuzzles[utcDate.DayNumber % orderedPuzzles.Count];
    }
}
