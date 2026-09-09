using System.Text.RegularExpressions;

namespace Palabravo.Core.Services;

public static partial class ReferralLink
{
    public static string? PuzzleFromCode(string? code)
    {
        var value = code?.Trim().ToLowerInvariant() ?? "";
        if (int.TryParse(value, out var number) && number is >= 1 and <= 30) return $"puzzle-{number:00}";
        return PuzzleId().IsMatch(value) ? value : null;
    }
    public static string? Parse(Uri uri) => uri.Scheme == "https" && uri.Host == "palabravo.app" &&
        uri.IsDefaultPort && uri.AbsolutePath.StartsWith("/reto/", StringComparison.Ordinal)
        ? PuzzleFromCode(uri.AbsolutePath[6..]) : null;
    public static string Build(string puzzleId) => $"https://palabravo.app/reto/{Uri.EscapeDataString(puzzleId)}";
    [GeneratedRegex("^puzzle-(0[1-9]|[12][0-9]|30)$")] private static partial Regex PuzzleId();
}
