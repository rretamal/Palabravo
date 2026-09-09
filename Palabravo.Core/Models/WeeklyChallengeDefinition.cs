using System.Text.RegularExpressions;

namespace Palabravo.Core.Models;

public sealed class WeeklyChallengeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Flag { get; set; } = "✦";
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public PuzzleDefinition Puzzle { get; set; } = new();
    public WeeklyBadgeDefinition Badge { get; set; } = new();
    public WeeklyShareDefinition Share { get; set; } = new();

    public bool IsActive(DateTimeOffset now) => StartsAt <= now && now < EndsAt;
}

public sealed class WeeklyBadgeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "✦";
    public string? ImageUrl { get; set; }
}

public sealed class WeeklyShareDefinition
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public static partial class WeeklyChallengeValidator
{
    public static void Validate(WeeklyChallengeDefinition weekly)
    {
        if (!SafeId().IsMatch(weekly.Id) || string.IsNullOrWhiteSpace(weekly.Title)
            || string.IsNullOrWhiteSpace(weekly.Subtitle) || !Country().IsMatch(weekly.Country)
            || string.IsNullOrWhiteSpace(weekly.Flag) || weekly.EndsAt <= weekly.StartsAt
            || weekly.EndsAt - weekly.StartsAt > TimeSpan.FromDays(31))
            throw new InvalidDataException("El reto semanal tiene metadatos inválidos.");
        if (!SafeId().IsMatch(weekly.Puzzle.Id) || weekly.Puzzle.ContentVersion < 1
            || string.IsNullOrWhiteSpace(weekly.Puzzle.Title) || weekly.Puzzle.Groups.Count != 4)
            throw new InvalidDataException("El puzzle semanal es inválido.");
        if (weekly.Puzzle.Groups.Any(group => string.IsNullOrWhiteSpace(group.Category)
                || string.IsNullOrWhiteSpace(group.Hint) || group.Words.Count != 4
                || group.Words.Any(string.IsNullOrWhiteSpace))
            || weekly.Puzzle.Groups.SelectMany(group => group.Words)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != 16)
            throw new InvalidDataException("El puzzle semanal debe contener cuatro grupos y dieciséis palabras únicas.");
        if (!SafeId().IsMatch(weekly.Badge.Id) || string.IsNullOrWhiteSpace(weekly.Badge.Name)
            || string.IsNullOrWhiteSpace(weekly.Badge.Icon) || string.IsNullOrWhiteSpace(weekly.Share.Title)
            || string.IsNullOrWhiteSpace(weekly.Share.Message))
            throw new InvalidDataException("El badge o los textos para compartir son inválidos.");
        if (weekly.Badge.ImageUrl is { Length: > 0 } image
            && (!Uri.TryCreate(image, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidDataException("La imagen del badge debe usar HTTPS.");
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{2,63}$")]
    private static partial Regex SafeId();
    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex Country();
}
