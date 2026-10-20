using System.Text.Json;
using Palabravo.Core.Models;

namespace Palabravo.Core.Services;

public static class DailyContentSchedule
{
    // An exact dated entry is immutable. Out-of-calendar play is clearly labeled as archive.
    public static PuzzleDefinition? Select(DailyChallengeCatalog catalog, DateOnly utcDate)
    {
        DailyChallengeValidator.Validate(catalog);
        var exact = catalog.Entries.FirstOrDefault(e => e.Date == utcDate);
        if (exact is not null) return exact.Puzzle;
        var available = catalog.Entries.Where(e => e.Date < utcDate).OrderBy(e => e.Date).ToList();
        if (available.Count == 0) return null; // Before activation, retain the legacy daily.
        var archived = available[utcDate.DayNumber % available.Count].Puzzle;
        var copy = JsonSerializer.Deserialize<PuzzleDefinition>(JsonSerializer.Serialize(archived))!;
        copy.Id = $"daily-{utcDate:yyyy-MM-dd}-archive";
        copy.Title = $"Del archivo · {archived.Title}";
        return copy;
    }

    public static string SaveSelection(DateOnly date, PuzzleDefinition puzzle) =>
        JsonSerializer.Serialize(new DailyChallengeEntry { Date = date, Puzzle = puzzle });

    public static PuzzleDefinition? ReadSelection(string json, DateOnly date)
    {
        try
        {
            var entry = JsonSerializer.Deserialize<DailyChallengeEntry>(json);
            if (entry is null || entry.Date != date) return null;
            PuzzleCatalogValidator.ValidatePuzzle(entry.Puzzle);
            return entry.Puzzle;
        }
        catch { return null; }
    }

    public static bool CanReplace(DailyChallengeCatalog previous, DailyChallengeCatalog next)
    {
        DailyChallengeValidator.Validate(next);
        var previousDates = previous.Entries.Select(e => e.Date).ToHashSet();
        var lastPublished = previous.Entries.Max(e => e.Date);
        return next.Version >= previous.Version
            && next.Entries.Where(e => !previousDates.Contains(e.Date)).All(e => e.Date > lastPublished)
            && previous.Entries.All(old => next.Entries.Any(e => e.Date == old.Date
                && JsonSerializer.Serialize(e) == JsonSerializer.Serialize(old)));
    }
}
