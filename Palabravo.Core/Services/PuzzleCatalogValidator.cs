using Palabravo.Core.Models;

namespace Palabravo.Core.Services;

public static class PuzzleCatalogValidator
{
    public static void Validate(IReadOnlyList<PuzzleDefinition> puzzles)
    {
        if (puzzles.Count < RankCatalog.TotalChallenges)
            throw new InvalidDataException($"El catálogo debe contener {RankCatalog.TotalChallenges} retos.");

        var ordered = puzzles.OrderBy(puzzle => puzzle.Order).ToList();
        if (ordered.Select(puzzle => puzzle.Order).Distinct().Count() != puzzles.Count
            || ordered.Select(puzzle => puzzle.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != puzzles.Count)
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
            if (puzzle.Dynamic != "connections")
            {
                if (!new[] { "bridge", "trivia", "pieces", "intruder", "visual", "recall" }.Contains(puzzle.Dynamic)
                    || puzzle.Questions.Count is < 2 or > 5
                    || puzzle.Questions.Any(q => string.IsNullOrWhiteSpace(q.Prompt)
                        || (puzzle.Dynamic == "recall" ? q.Options.Count != 0 || string.IsNullOrWhiteSpace(q.Answer)
                            || q.Answer.Length > 80 || q.AcceptedAnswers.Any(a => string.IsNullOrWhiteSpace(a) || a.Length > 80)
                            : q.Options.Count != 4 || q.Options.Any(string.IsNullOrWhiteSpace) || q.Options.Distinct().Count() != 4 || !q.Options.Contains(q.Answer))
                        || string.IsNullOrWhiteSpace(q.Explanation)
                        || string.IsNullOrWhiteSpace(q.Hint)))
                    throw new InvalidDataException($"El reto {puzzle.Id} tiene preguntas inválidas.");
                if (puzzle.Dynamic == "pieces" && puzzle.Questions.Any(q => q.Fragments.Count != 4
                    || q.Fragments.Distinct().Count() != 4 || q.SolutionParts.Count != 2
                    || q.SolutionParts.Any(part => !q.Fragments.Contains(part))
                    || !string.Equals(string.Concat(q.SolutionParts), q.Answer, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException($"El reto {puzzle.Id} tiene piezas inválidas.");
                continue;
            }
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
