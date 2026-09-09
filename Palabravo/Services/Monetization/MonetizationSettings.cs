using System.Text.Json;

namespace Palabravo.Services.Monetization;

public sealed record MonetizationSettings
{
    public bool Enabled { get; init; }
    public bool TestAds { get; init; } = true;
    public string AndroidInterstitialId { get; init; } = "";
    public string AndroidRewardedId { get; init; } = "";
    public string IosInterstitialId { get; init; } = "";
    public string IosRewardedId { get; init; } = "";
    public string ApiBaseUrl { get; init; } = "https://palabravo.app/api/";
    public static MonetizationSettings Current { get; } = Load();
    private static MonetizationSettings Load()
    {
        using var stream = typeof(MonetizationSettings).Assembly.GetManifestResourceStream("Palabravo.monetization.json")!;
        var settings = JsonSerializer.Deserialize<MonetizationSettings>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        if (!Uri.TryCreate(settings.ApiBaseUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            throw new InvalidDataException("La API de monetización debe usar HTTPS.");
        if (settings.Enabled && !settings.TestAds && new[] { settings.AndroidInterstitialId, settings.AndroidRewardedId, settings.IosInterstitialId, settings.IosRewardedId }.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Faltan unidades de anuncios de producción.");
        return settings;
    }
}
