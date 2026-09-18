using System.Text.Json;
using System.Text.Json.Serialization;

namespace Palabravo.Core.Monetization;

public sealed record MonetizationConfig
{
    public const string ProductId = "palabravo_remove_ads";
    public const int DailyHints = 3;
    [JsonPropertyName("config_version")] public string Version { get; init; } = "launch-1";
    [JsonPropertyName("interstitial_enabled")] public bool InterstitialEnabled { get; init; } = true;
    [JsonPropertyName("banner_enabled")] public bool BannerEnabled { get; init; } = true;
    [JsonPropertyName("rewarded_enabled")] public bool RewardedEnabled { get; init; } = true;
    [JsonPropertyName("interstitial_every_completed")] public int EveryCompleted { get; init; } = 3;
    [JsonPropertyName("interstitial_grace_completed")] public int GraceCompleted { get; init; } = 2;
    [JsonPropertyName("min_active_seconds_between_ads")] public int MinActiveSeconds { get; init; } = 180;
    [JsonPropertyName("max_interstitials_per_session")] public int MaxPerSession { get; init; } = 2;
    [JsonPropertyName("first_referred_attempt_ad_free")] public bool FirstReferredAdFree { get; init; } = true;
    [JsonPropertyName("remove_ads_product_id")] public string RemoveAdsProductId { get; init; } = ProductId;
    [JsonPropertyName("experiment_enabled")] public bool ExperimentEnabled { get; init; }

    public bool IsValid => !string.IsNullOrWhiteSpace(Version) && Version.Length <= 64 &&
        EveryCompleted is >= 3 and <= 30 && GraceCompleted is >= 2 and <= 100 &&
        MinActiveSeconds is >= 180 and <= 3600 && MaxPerSession is >= 0 and <= 2 &&
        RemoveAdsProductId == ProductId;

    public static MonetizationConfig? Parse(string json)
    {
        try
        {
            var config = JsonSerializer.Deserialize<MonetizationConfig>(json);
            return config is { IsValid: true } ? config : null;
        }
        catch (JsonException) { return null; }
    }
}

public sealed class MonetizationState
{
    public string InstallationId { get; set; } = Guid.NewGuid().ToString("N");
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset LastInteractionUtc { get; set; }
    public double ActiveSeconds { get; set; }
    public double? LastAdActiveSeconds { get; set; }
    public int SessionInterstitials { get; set; }
    public long CompletedAttempts { get; set; }
    public long? CompletedAttemptsAtLastInterstitial { get; set; }
    public string? LastCompletedAttempt { get; set; }
    public string? AttemptId { get; set; }
    public string? PuzzleId { get; set; }
    public bool ReferredExemptionUsed { get; set; }
    public bool AttemptAdFree { get; set; }
    public int WelcomeHints { get; set; } = 1;
    public DateOnly RetryDate { get; set; }
    public Dictionary<string, int> FreeRetriesUsed { get; set; } = [];
    public HashSet<string> FailedPuzzles { get; set; } = [];
    public Dictionary<string, string> RetryAdRequests { get; set; } = [];
    public Dictionary<string, int> PendingRetries { get; set; } = [];
    public DateOnly DailyDate { get; set; }
    public int DailyHintsUsed { get; set; }
    public bool OwnsRemoveAds { get; set; }
    public bool EntitlementResolved { get; set; }
    public HashSet<string> EarnedRewards { get; set; } = [];
    public List<string> PendingRewards { get; set; } = [];
    public string? UnconfirmedHintReward { get; set; }
    public HashSet<string> Impressions { get; set; } = [];
    public MonetizationConfig EffectiveConfig { get; set; } = new();
    public MonetizationConfig? AttemptConfig { get; set; }
    public MonetizationConfig CachedConfig { get; set; } = new();
    public bool InterstitialKilled { get; set; }
    public bool BannerKilled { get; set; }
    public bool RewardedKilled { get; set; }
    public string Variant { get; set; } = Random.Shared.Next(2) == 0 ? "rewarded_only" : "moderate_interstitials";
}
