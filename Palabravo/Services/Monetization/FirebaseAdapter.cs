#if ANDROID || IOS
using Palabravo.Core.Monetization;
using Plugin.Firebase.Analytics;
using Plugin.Firebase.RemoteConfig;

namespace Palabravo.Services.Monetization;

public sealed class FirebaseAdapter(IMonetizationStore store) : IMonetizationConfiguration, IMonetizationTelemetry
{
    private static readonly SemaphoreSlim IdentityLock = new(1, 1);
    public static bool Ready { get; set; }
    public async Task<MonetizationConfig?> FetchAsync()
    {
        if (!Ready) return null;
        await CrossFirebaseRemoteConfig.Current.FetchAsync(60).WaitAsync(TimeSpan.FromSeconds(10));
        await CrossFirebaseRemoteConfig.Current.ActivateAsync();
        return MonetizationConfig.Parse(CrossFirebaseRemoteConfig.Current.GetString("monetization_policy"));
    }
    public void Track(string name, IReadOnlyDictionary<string, object> parameters)
    {
        try { TrackCore(name, parameters); } catch { /* Measurement must not block the UI. */ }
    }
    private void TrackCore(string name, IReadOnlyDictionary<string, object> parameters)
    {
        if (!Ready || !Preferences.Default.Get("analytics_consent", false)) return;
        var data = parameters.ToDictionary(x => x.Key, x => x.Value is bool b ? (object)(b ? 1L : 0L) : x.Value);
        store.Update(s =>
        {
            data.TryAdd("event_id", Guid.NewGuid().ToString("N"));
            data.TryAdd("event_schema_version", 1L);
            data.TryAdd("session_id", s.SessionId);
            data.TryAdd("attempt_id", s.AttemptId ?? "");
            data.TryAdd("puzzle_id", s.PuzzleId ?? "");
            data.TryAdd("config_version", (s.AttemptConfig ?? s.EffectiveConfig).Version);
            data.TryAdd("active_seconds", s.ActiveSeconds);
            return true;
        });
        data["environment"] = MonetizationSettings.Current.TestAds ? "test" : "production";
        data["platform"] = DeviceInfo.Platform.ToString().ToLowerInvariant();
        data["app_version"] = AppInfo.VersionString;
        CrossFirebaseAnalytics.Current.LogEvent(name == "session_start" ? "play_session_start" : name, data);
    }
    public static void SetCollection(bool enabled)
    {
        Preferences.Default.Set("analytics_consent", enabled);
        if (!Ready) return;
        CrossFirebaseAnalytics.Current.IsAnalyticsCollectionEnabled = enabled;
        Plugin.Firebase.Crashlytics.CrossFirebaseCrashlytics.Current.SetCrashlyticsCollectionEnabled(enabled);
        // Do not discard the identifier needed to request historical deletion.
        if (enabled)
        {
            Preferences.Default.Set("analytics_used", true);
            _ = CaptureInstanceIdsAsync();
        }
    }

    public static async Task<string[]> CaptureInstanceIdsAsync()
    {
        await IdentityLock.WaitAsync();
        try
        {
        var ids = System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(Preferences.Default.Get("analytics_instance_ids", "[]")) ?? [];
        if (Ready && Preferences.Default.Get("analytics_used", false))
        {
            var id = await CrossFirebaseAnalytics.Current.GetAppInstanceIdAsync().WaitAsync(TimeSpan.FromSeconds(5));
            if (!string.IsNullOrWhiteSpace(id)) ids.Add(id);
        }
        Preferences.Default.Set("analytics_instance_ids", System.Text.Json.JsonSerializer.Serialize(ids));
        return ids.ToArray();
        }
        finally { IdentityLock.Release(); }
    }

    public static async Task ClearLocalDataAsync()
    {
        SetCollection(false);
        await IdentityLock.WaitAsync();
        try
        {
        Preferences.Default.Remove("analytics_instance_ids");
        Preferences.Default.Remove("analytics_used");
        if (!Ready) return;
        CrossFirebaseAnalytics.Current.ResetAnalyticsData();
        Plugin.Firebase.Crashlytics.CrossFirebaseCrashlytics.Current.DeleteUnsentReports();
        }
        finally { IdentityLock.Release(); }
    }
}
#endif
