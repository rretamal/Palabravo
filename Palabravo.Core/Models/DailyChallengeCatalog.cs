using Palabravo.Core.Services;

namespace Palabravo.Core.Models;

public sealed class DailyChallengeCatalog
{
    public int Version { get; set; } = 1;
    public string TimeZone { get; set; } = "UTC";
    public List<DailyChallengeEntry> Entries { get; set; } = [];
}

public sealed class DailyChallengeEntry
{
    public DateOnly Date { get; set; }
    public PuzzleDefinition Puzzle { get; set; } = new();
}

public static class DailyChallengeValidator
{
    public static void Validate(DailyChallengeCatalog catalog)
    {
        if (catalog.Version < 1 || catalog.TimeZone != "UTC" || catalog.Entries.Count == 0
            || catalog.Entries.Select(e => e.Date).Distinct().Count() != catalog.Entries.Count
            || catalog.Entries.Select(e => e.Puzzle.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != catalog.Entries.Count)
            throw new InvalidDataException("El calendario diario es inválido.");
        foreach (var entry in catalog.Entries)
        {
            if (entry.Puzzle.Id != $"daily-{entry.Date:yyyy-MM-dd}")
                throw new InvalidDataException("La identidad diaria debe coincidir con su fecha.");
            PuzzleCatalogValidator.ValidatePuzzle(entry.Puzzle);
        }
    }
}
