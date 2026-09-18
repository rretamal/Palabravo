using Palabravo.Core.Monetization;

namespace Palabravo.Tests;

public sealed class MonetizationTests
{
    [Fact]
    public async Task Queued_ads_recheck_ownership_before_request_and_show()
    {
        var f = await Fixture.Create();
        f.Ads.Requests = 0; f.Ads.QueueLoads = true;
        f.Service.BeginAttempt("a", "p");
        f.Store.State.OwnsRemoveAds = true;
        foreach (var load in f.Ads.PendingLoads) load();
        Assert.Equal(0, f.Ads.Requests);

        f.Store.State.OwnsRemoveAds = false;
        f.Store.State.CompletedAttempts = 5;
        f.Advance(301);
        f.Ads.BeforeShow = () => f.Store.State.OwnsRemoveAds = true;
        await f.Service.CompleteAttemptAsync("a", true);
        Assert.Equal(0, f.Ads.Shown);
    }

    [Fact]
    public async Task Deletion_clears_identifying_state_without_removing_a_paid_benefit()
    {
        var f = await Fixture.Create();
        f.Service.BeginAttempt("attempt", "puzzle");
        var oldId = f.Store.State.InstallationId;
        f.Store.State.OwnsRemoveAds = true;
        f.Store.State.DailyHintsUsed = 2;
        f.Store.State.PendingRewards.Add("reward");
        await f.Service.ClearPersonalDataAsync();
        Assert.NotEqual(oldId, f.Store.State.InstallationId);
        Assert.Null(f.Store.State.AttemptId);
        Assert.Null(f.Store.State.PuzzleId);
        Assert.Empty(f.Store.State.PendingRewards);
        Assert.True(f.Store.State.OwnsRemoveAds);
        Assert.Equal(2, f.Store.State.DailyHintsUsed);
    }
    [Fact]
    public async Task Interstitials_start_at_three_and_are_limited_to_two_per_session()
    {
        var f = await Fixture.Create();
        for (var i = 1; i <= 12; i++)
        {
            f.Service.BeginAttempt($"a{i}", "puzzle");
            f.Advance(60);
            await f.Service.CompleteAttemptAsync($"a{i}", true);
            await f.Service.CompleteAttemptAsync($"a{i}", true);
            Assert.Equal(i < 3 ? 0 : i < 6 ? 1 : 2, f.Ads.Shown);
        }
        Assert.Equal(12, f.Store.State.CompletedAttempts);
    }

    [Fact]
    public async Task Unavailable_opportunity_is_not_shown_late_and_failure_does_not_count()
    {
        var f = await Fixture.Create();
        f.Store.State.CompletedAttempts = 5;
        f.Service.BeginAttempt("a", "p"); f.Advance(301);
        f.Ads.Ready = false;
        await f.Service.CompleteAttemptAsync("a", true);
        f.Ads.Ready = true;
        await f.Service.CompleteAttemptAsync("a", true);
        f.Service.BeginAttempt("b", "p");
        await f.Service.CompleteAttemptAsync("b", false);
        Assert.Equal(0, f.Ads.Shown);
        Assert.Equal(6, f.Store.State.CompletedAttempts);
        f.Service.BeginAttempt("c", "p");
        await f.Service.CompleteAttemptAsync("c", true);
        Assert.Equal(1, f.Ads.Shown);
        Assert.Equal(7, f.Store.State.CompletedAttemptsAtLastInterstitial);
        for (var i = 8; i <= 10; i++)
        {
            f.Service.BeginAttempt($"a{i}", "p");
            f.Advance(60);
            await f.Service.CompleteAttemptAsync($"a{i}", true);
            Assert.Equal(i < 10 ? 1 : 2, f.Ads.Shown);
        }
    }

    [Fact]
    public async Task First_interstitial_needs_no_initial_playtime_but_subsequent_one_needs_180_seconds()
    {
        var f = await Fixture.Create();
        for (var i = 1; i <= 6; i++)
        {
            f.Service.BeginAttempt($"a{i}", "p");
            await f.Service.CompleteAttemptAsync($"a{i}", true);
            Assert.Equal(i < 3 ? 0 : 1, f.Ads.Shown);
        }
        f.Service.BeginAttempt("a7", "p");
        f.Advance(179);
        await f.Service.CompleteAttemptAsync("a7", true);
        Assert.Equal(1, f.Ads.Shown);
        f.Service.BeginAttempt("a8", "p");
        f.Advance(1);
        await f.Service.CompleteAttemptAsync("a8", true);
        Assert.Equal(2, f.Ads.Shown);
    }

    [Fact]
    public async Task Rewarded_impression_delays_even_the_first_interstitial()
    {
        var f = await Fixture.Create();
        f.Ads.Emit(new("ad_impression", AdFormat.Rewarded, "rewarded"));
        f.Store.State.CompletedAttempts = 2;
        f.Service.BeginAttempt("a", "p");
        f.Advance(179);
        await f.Service.CompleteAttemptAsync("a", true);
        Assert.Equal(0, f.Ads.Shown);
        f.Service.BeginAttempt("b", "p");
        f.Advance(1);
        await f.Service.CompleteAttemptAsync("b", true);
        Assert.Equal(1, f.Ads.Shown);
    }

    [Fact]
    public async Task First_referred_attempt_is_exempt_even_at_six()
    {
        var f = await Fixture.Create();
        f.Store.State.CompletedAttempts = 5;
        f.Service.BeginAttempt("a", "p", true); f.Advance(301);
        await f.Service.CompleteAttemptAsync("a", true);
        Assert.Equal(0, f.Ads.Shown);
        Assert.True(f.Store.State.ReferredExemptionUsed);
    }

    [Fact]
    public async Task Restart_preserves_session_cap_and_cooldown()
    {
        var f = await Fixture.Create();
        f.Store.State.CompletedAttempts = 5;
        f.Service.BeginAttempt("a", "p"); f.Advance(301);
        await f.Service.CompleteAttemptAsync("a", true);
        var second = f.NewService();
        second.Activity(false);
        Assert.Equal(1, f.Store.State.SessionInterstitials);
        Assert.Equal(6, f.Store.State.CompletedAttemptsAtLastInterstitial);
        var session = f.Store.State.SessionId;
        f.Clock.Now += TimeSpan.FromMinutes(31);
        second.Activity(false);
        Assert.NotEqual(session, f.Store.State.SessionId);
        Assert.Equal(0, f.Store.State.SessionInterstitials);
        f.Store.State.WelcomeHints = 0;
        Assert.Equal(HintSource.Unavailable, second.GetHintOffer(true).Source);
    }

    [Fact]
    public async Task Duplicate_reward_survives_close_and_is_consumed_once()
    {
        var f = await Fixture.Create();
        f.Store.State.WelcomeHints = 0;
        f.Service.BeginAttempt("a", "p");
        f.Ads.Emit(new("ad_dismissed", AdFormat.Rewarded, "ad"));
        f.Ads.Emit(new("reward_earned", AdFormat.Rewarded, "ad", RewardId: "r"));
        f.Ads.Emit(new("reward_earned", AdFormat.Rewarded, "ad", RewardId: "r"));
        var restarted = f.NewService();
        restarted.BeginAttempt("b", "p");
        Assert.True(restarted.ConsumeHint("b", () => true));
        Assert.False(restarted.ConsumeHint("b", () => true));
        Assert.Empty(f.Store.State.PendingRewards);
    }

    [Fact]
    public async Task Failed_delivery_and_stale_attempt_do_not_consume_a_hint()
    {
        var f = await Fixture.Create();
        f.Service.BeginAttempt("a", "p");
        Assert.False(f.Service.ConsumeHint("old", () => true));
        Assert.False(f.Service.ConsumeHint("a", () => false));
        Assert.Equal(1, f.Store.State.WelcomeHints);
        Assert.Equal(HintSource.Unavailable, f.Service.GetHintOffer(false).Source);
    }

    [Fact]
    public async Task Buyer_gets_daily_before_welcome_and_never_gets_ads()
    {
        var f = await Fixture.Create();
        f.Store.State.OwnsRemoveAds = true;
        f.Service.BeginAttempt("a", "p");
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HintSource.Daily, f.Service.GetHintOffer(true).Source);
            Assert.True(f.Service.ConsumeHint("a", () => true));
        }
        Assert.Equal(1, f.Store.State.WelcomeHints);
        Assert.True(f.Service.ConsumeHint("a", () => true));
        Assert.Equal(HintSource.Unavailable, f.Service.GetHintOffer(true).Source);
        f.Clock.Now += TimeSpan.FromDays(1);
        Assert.Equal(HintSource.Daily, f.Service.GetHintOffer(true).Source);
    }

    [Fact]
    public async Task Configuration_changes_wait_for_session_and_kill_is_immediate()
    {
        var f = await Fixture.Create();
        f.Config.Value = new() { Version = "next", EveryCompleted = 6, RewardedEnabled = false };
        await f.Service.InitializeAsync();
        Assert.Equal(3, f.Store.State.EffectiveConfig.EveryCompleted);
        Assert.True(f.Store.State.RewardedKilled);
        f.Clock.Now += TimeSpan.FromMinutes(31);
        f.Service.Activity(false);
        Assert.Equal(6, f.Store.State.EffectiveConfig.EveryCompleted);
        f.Config.Value = new() { MinActiveSeconds = 0 };
        await f.Service.InitializeAsync();
        Assert.Equal("next", f.Store.State.CachedConfig.Version);
    }

    [Fact]
    public async Task Consent_denial_and_unknown_ownership_suppress_requests()
    {
        var f = await Fixture.Create();
        f.Consent.Allowed = false;
        f.Ads.Requests = 0;
        f.Service.BeginAttempt("a", "p");
        Assert.Equal(0, f.Ads.Requests);
        f.Consent.Allowed = true;
        f.Store.State.EntitlementResolved = false;
        f.Service.BeginAttempt("b", "p");
        Assert.Equal(0, f.Ads.Requests);
    }

    [Fact]
    public async Task Banner_respects_consent_ownership_kill_switch_and_full_screen_cooldown()
    {
        var f = await Fixture.Create();
        Assert.True(f.Service.CanShowBanner);
        f.Consent.Allowed = false;
        Assert.False(f.Service.CanShowBanner);
        f.Consent.Allowed = true;
        f.Store.State.OwnsRemoveAds = true;
        Assert.False(f.Service.CanShowBanner);
        f.Store.State.OwnsRemoveAds = false;
        f.Store.State.BannerKilled = true;
        Assert.False(f.Service.CanShowBanner);
        f.Store.State.BannerKilled = false;
        f.Ads.Emit(new("ad_impression", AdFormat.Interstitial, "full-screen"));
        Assert.False(f.Service.CanShowBanner);
        f.Advance(180);
        Assert.True(f.Service.CanShowBanner);
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("{\"min_active_seconds_between_ads\":0}")]
    [InlineData("{\"remove_ads_product_id\":\"other\"}")]
    public void Invalid_config_is_rejected(string json) => Assert.Null(MonetizationConfig.Parse(json));

    private sealed class Fixture
    {
        public MemoryStore Store = new();
        public FakeAds Ads = new();
        public FakeConsent Consent = new();
        public FakeConfig Config = new();
        public FakeClock Clock = new();
        public MonetizationService Service = null!;
        public MonetizationService NewService() => new(Store, Ads, Consent, new FakePurchases(), new FakeGateway(), Config, new FakeTelemetry(), Clock);
        public static async Task<Fixture> Create()
        {
            var f = new Fixture(); f.Service = f.NewService(); await f.Service.InitializeAsync(); return f;
        }
        public void Advance(int seconds) { Service.Activity(true); Clock.Now += TimeSpan.FromSeconds(seconds); Service.Activity(false); }
    }
    [Fact]
    public async Task Retry_allowance_survives_restart_and_renews_next_day()
    {
        var f = await Fixture.Create();
        f.Service.BeginAttempt("failed", "p");
        await f.Service.CompleteAttemptAsync("failed", false);
        Assert.True(await f.Service.AuthorizeRetryAsync("p"));
        var restarted = f.NewService();
        Assert.True(await restarted.AuthorizeRetryAsync("p"));
        Assert.True(restarted.RetryNeedsAd("p"));
        Assert.False(await restarted.AuthorizeRetryAsync("p"));
        Assert.True(await restarted.AuthorizeRetryAsync("other"));
        f.Clock.Now += TimeSpan.FromDays(1);
        Assert.False(restarted.RetryNeedsAd("p"));
        Assert.True(await restarted.AuthorizeRetryAsync("p"));
        f.Store.State.OwnsRemoveAds = true;
        for (var i = 0; i < 4; i++) Assert.True(await restarted.AuthorizeRetryAsync("p"));
    }

    [Fact]
    public async Task Retry_reward_is_not_a_hint_and_duplicate_callbacks_do_not_grant_extra_retries()
    {
        var f = await Fixture.Create();
        f.Store.State.FailedPuzzles.Add("p");
        await f.Service.AuthorizeRetryAsync("p");
        await f.Service.AuthorizeRetryAsync("p");
        f.Ads.Emit(new("ad_request", AdFormat.Rewarded, "retry-ad"));
        Assert.False(await f.Service.AuthorizeRetryAsync("p"));
        var signal = new AdSignal("reward_earned", AdFormat.Rewarded, "retry-ad", RewardId: "retry-reward");
        f.Ads.Emit(signal);
        f.Ads.Emit(signal);
        Assert.Empty(f.Store.State.PendingRewards);
        Assert.True(await f.Service.AuthorizeRetryAsync("p"));
        Assert.Equal(0, f.Store.State.PendingRetries["p"]);
        Assert.True(f.Service.RetryNeedsAd("p"));
    }

    [Fact]
    public async Task Only_one_welcome_hint_is_free()
    {
        var f = await Fixture.Create();
        f.Service.BeginAttempt("a", "p");
        Assert.True(f.Service.ConsumeHint("a", () => true));
        f.Service.ConfirmHintDisplayed();
        Assert.Equal(HintSource.Advertisement, f.Service.GetHintOffer(true).Source);
        Assert.False(f.Service.ConsumeHint("a", () => true));
    }

    private sealed class MemoryStore : IMonetizationStore
    {
        public MonetizationState State = new();
        public T Update<T>(Func<MonetizationState, T> update) => update(State);
    }
    private sealed class FakeClock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class FakeAds : IAdAdapter
    {
        public event Action<AdSignal>? Signal;
        public int Shown; public int Requests; public bool Ready = true;
        public bool QueueLoads;
        public List<Action> PendingLoads = [];
        public Action? BeforeShow;
        public bool IsReady(AdFormat format) => Ready;
        public void Preload(AdFormat format, Func<bool> isAllowed)
        {
            void Load() { if (isAllowed()) Requests++; }
            if (QueueLoads) PendingLoads.Add(Load); else Load();
        }
        public Task ShowAsync(AdFormat format, Func<bool> isAllowed)
        {
            BeforeShow?.Invoke();
            if (isAllowed()) { Shown++; Emit(new("ad_impression", format, Guid.NewGuid().ToString())); }
            return Task.CompletedTask;
        }
        public void Discard() { }
        public void Emit(AdSignal signal) => Signal?.Invoke(signal);
    }
    private sealed class FakeConsent : IConsentAdapter
    {
        public bool Allowed = true;
        public bool CanRequestAds => Allowed;
        public Task RefreshAsync() => Task.CompletedTask;
        public Task ShowPrivacyOptionsAsync() => Task.CompletedTask;
    }
    private sealed class FakeConfig : IMonetizationConfiguration
    {
        public MonetizationConfig Value = new();
        public Task<MonetizationConfig?> FetchAsync() => Task.FromResult<MonetizationConfig?>(Value);
    }
    private sealed class FakePurchases : IPurchaseAdapter
    {
        public event Action? Changed { add { } remove { } }
        public Task<StoreProduct?> GetProductAsync() => Task.FromResult<StoreProduct?>(null);
        public Task<PurchaseOutcome> PurchaseAsync() => Task.FromResult(new PurchaseOutcome(PurchaseStatus.Cancelled));
        public Task<IReadOnlyList<PurchaseProof>> RestoreAsync(bool userInitiated = false) => Task.FromResult<IReadOnlyList<PurchaseProof>>([]);
        public Task FinishAsync(PurchaseProof proof) => Task.CompletedTask;
    }
    private sealed class FakeGateway : IEntitlementGateway
    {
        public Task<Entitlement> VerifyAsync(PurchaseProof proof) => Task.FromResult(new Entitlement(true, true, "store"));
        public Task<Entitlement> GetAsync() => Task.FromResult(new Entitlement(true, false, "server"));
    }
    private sealed class FakeTelemetry : IMonetizationTelemetry
    {
        public void Track(string name, IReadOnlyDictionary<string, object> parameters) { }
    }
}
