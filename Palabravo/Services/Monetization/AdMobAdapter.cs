#if ANDROID || IOS
using Palabravo.Core.Monetization;
using Plugin.AdMob;
using Plugin.AdMob.Services;

namespace Palabravo.Services.Monetization;

public sealed class AdMobAdapter(IInterstitialAdService interstitials, IRewardedAdService rewarded) : IAdAdapter
{
    private IInterstitialAd? interstitial;
    private IRewardedAd? reward;
    private TaskCompletionSource? showing;
    private readonly Dictionary<AdFormat, DateTimeOffset> loaded = [];
    private readonly Dictionary<AdFormat, (string Id, DateTimeOffset Requested)> requests = [];
    private readonly Dictionary<AdFormat, TaskCompletionSource> completions = [];
    public event Action<AdSignal>? Signal;

    public bool IsReady(AdFormat format) => loaded.TryGetValue(format, out var at) && DateTimeOffset.UtcNow - at < TimeSpan.FromMinutes(50) &&
        (format == AdFormat.Rewarded ? reward?.IsLoaded == true : interstitial?.IsLoaded == true);

    public void Preload(AdFormat format, Func<bool> isAllowed)
    {
        if (!MainThread.IsMainThread) { MainThread.BeginInvokeOnMainThread(() => Preload(format, isAllowed)); return; }
        if (!isAllowed()) return;
        if (requests.TryGetValue(format, out var previous))
        {
            if (IsReady(format) || (!loaded.ContainsKey(format) && DateTimeOffset.UtcNow - previous.Requested < TimeSpan.FromSeconds(60))) return;
            Clear(format);
        }
        var id = Guid.NewGuid().ToString("N");
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        completions[format] = completion;
        requests[format] = (id, DateTimeOffset.UtcNow);
        bool Current() => requests.TryGetValue(format, out var request) && request.Id == id;
        void Emit(string name, string? error = null) => Signal?.Invoke(new(name, format, id, error));
        void Loaded() { if (!Current()) return; loaded[format] = DateTimeOffset.UtcNow; Emit("ad_load_result"); }
        void FailedLoad() { if (Current()) Clear(format); Emit("ad_load_result", "load_failed"); }
        void Closed() { if (Current()) Clear(format); Emit("ad_dismissed"); completion.TrySetResult(); }
        void FailedShow() { if (Current()) Clear(format); Emit("ad_show_failed", "show_failed"); completion.TrySetResult(); }
        if (format == AdFormat.Rewarded)
        {
            reward = rewarded.CreateAd();
            reward.OnAdLoaded += (_, _) => Loaded();
            reward.OnAdFailedToLoad += (_, _) => FailedLoad();
            reward.OnAdImpression += (_, _) => Emit("ad_impression");
            reward.OnAdFailedToShow += (_, _) => FailedShow();
            reward.OnAdDismissed += (_, _) => Closed();
            // This handler deliberately survives dismissal: SDK callback ordering may vary.
            reward.OnUserEarnedReward += (_, _) => Signal?.Invoke(new("reward_earned", format, id, RewardId: id));
            Emit("ad_request"); reward.Load();
        }
        else
        {
            interstitial = interstitials.CreateAd();
            interstitial.OnAdLoaded += (_, _) => Loaded();
            interstitial.OnAdFailedToLoad += (_, _) => FailedLoad();
            interstitial.OnAdImpression += (_, _) => Emit("ad_impression");
            interstitial.OnAdFailedToShow += (_, _) => FailedShow();
            interstitial.OnAdDismissed += (_, _) => Closed();
            Emit("ad_request"); interstitial.Load();
        }
    }

    public Task ShowAsync(AdFormat format, Func<bool> isAllowed) => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        if (showing is not null || !IsReady(format) || !isAllowed()) return;
        showing = completions[format];
        try
        {
            if (format == AdFormat.Rewarded) reward!.Show(); else interstitial!.Show();
            await showing.Task;
        }
        finally { showing = null; }
    });

    private void Clear(AdFormat format)
    {
        loaded.Remove(format);
        requests.Remove(format);
        completions.Remove(format);
        if (format == AdFormat.Rewarded) reward = null; else interstitial = null;
    }
    public void Discard() { Clear(AdFormat.Rewarded); Clear(AdFormat.Interstitial); }
}

public sealed class AdMobConsentAdapter(IAdConsentService consent, Core.Services.GameplayActivity activity) : IConsentAdapter
{
    public bool CanRequestAds => consent.CanRequestAds();
    public Task RefreshAsync() => RunAsync(false);
    public Task ShowPrivacyOptionsAsync() => RunAsync(true);
    private Task RunAsync(bool privacy) => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        if (privacy && !consent.IsPrivacyOptionsRequired()) return;
        using var pause = activity.Pause();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Closed(object? sender, EventArgs e) => completion.TrySetResult();
        void Updated(object? sender, IConsentInformation? e) => completion.TrySetResult();
        void Failed(object? sender, IConsentError e) => completion.TrySetResult();
        consent.OnConsentFormDismissed += Closed;
        consent.OnConsentInfoUpdated += Updated;
        consent.OnConsentInfoFailedToUpdate += Failed;
        consent.OnConsentFormError += Failed;
        try
        {
            if (privacy) consent.ShowPrivacyOptionsForm(); else consent.LoadAndShowConsentFormIfRequired();
            await completion.Task;
        }
        finally
        {
            consent.OnConsentFormDismissed -= Closed;
            consent.OnConsentInfoUpdated -= Updated;
            consent.OnConsentInfoFailedToUpdate -= Failed;
            consent.OnConsentFormError -= Failed;
        }
    });
}
#endif
