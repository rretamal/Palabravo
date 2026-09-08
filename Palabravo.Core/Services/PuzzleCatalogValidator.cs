using Palabravo.Core.Models;

namespace Palabravo.Core.Services;

public static class PuzzleCatalogValidator
{
    public static void Validate(IReadOnlyList<PuzzleDefinition> puzzles)
    {
        if (puzzles.Count != RankCatalog.TotalChallenges)
            throw new InvalidDataException($"El catálogo debe contener {RankCatalog.TotalChallenges} retos.");

        var ordered = puzzles.OrderBy(puzzle => puzzle.Order).ToList();
        if (ordered.Select(puzzle => puzzle.Order).Distinct().Count() != RankCatalog.TotalChallenges
            || ordered.Select(puzzle => puzzle.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != RankCatalog.TotalChallenges)
            throw new InvalidDataException("Los IDs y órdenes de los retos deben ser únicos.");

        for (var index = 0; index < ordered.Count; index++)
        {
            var puzzle = ordered[index];
            var expectedOrder = index + 1;
            if (puzzle.Order != expectedOrder || puzzle.Id != $"puzzle-{expectedOrder:00}")
                throw new InvalidDataException($"El reto {expectedOrder} debe usar el ID puzzle-{expectedOrder:00}.");
            if (puzzle.Rank != RankCatalog.ForChallenge(expectedOrder).Tier)
                throw new InvalidDataException($"El reto {puzzle.Id} no pertenece al rango esperado.");
            if (puzzle.ContentVersion < 1 || string.IsNullOrWhiteSpace(puzzle.Title) || string.IsNullOrWhiteSpace(puzzle.Difficulty))
                throw new InvalidDataException($"El reto {puzzle.Id} tiene metadatos incompletos.");
            if (puzzle.Groups.Count != 4)
                throw new InvalidDataException($"El reto {puzzle.Id} debe contener cuatro grupos.");

            foreach (var group in puzzle.Groups)
            {
                if (string.IsNullOrWhiteSpace(group.Category) || string.IsNullOrWhiteSpace(group.Hint)
                    || group.Words.Count != 4 || group.Words.Any(string.IsNullOrWhiteSpace))
                    throw new InvalidDataException($"El reto {puzzle.Id} contiene un grupo inválido.");
            }

            var words = puzzle.Groups.SelectMany(group => group.Words).ToList();
            if (words.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 16)
                throw new InvalidDataException($"El reto {puzzle.Id} debe contener dieciséis palabras únicas.");
        }
    }
}
