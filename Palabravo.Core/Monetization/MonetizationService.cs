namespace Palabravo.Core.Monetization;

public sealed class MonetizationService : IMonetizationService
{
    private readonly IMonetizationStore store;
    private readonly IAdAdapter ads;
    private readonly IConsentAdapter consent;
    private readonly IPurchaseAdapter purchases;
    private readonly IEntitlementGateway gateway;
    private readonly IMonetizationConfiguration configuration;
    private readonly IMonetizationTelemetry telemetry;
    private readonly TimeProvider time;
    private readonly SemaphoreSlim operation = new(1, 1);
    private DateTimeOffset? activeSince;
    private HintOffer? visibleOffer;
    private DateTimeOffset lastConfigFetch;
    private bool fetchingConfig;

    public MonetizationService(IMonetizationStore store, IAdAdapter ads, IConsentAdapter consent,
        IPurchaseAdapter purchases, IEntitlementGateway gateway, IMonetizationConfiguration configuration,
        IMonetizationTelemetry telemetry, TimeProvider? time = null, bool enabled = true)
    {
        this.store = store; this.ads = ads; this.consent = consent; this.purchases = purchases;
        this.gateway = gateway; this.configuration = configuration; this.telemetry = telemetry;
        this.time = time ?? TimeProvider.System; IsEnabled = enabled;
        ads.Signal += OnAdSignal;
        purchases.Changed += () => _ = ReconcileUpdateAsync();
    }

    public bool IsEnabled { get; }
    public bool OwnsRemoveAds => store.Update(s => s.OwnsRemoveAds);

    public async Task InitializeAsync()
    {
        if (!IsEnabled || !await operation.WaitAsync(0)) return;
        try
        {
            Activity(false);
            // Retry interrupted payments and reconcile revocations on foregrounding.
            await ReconcileAsync();
            await RefreshConfigurationAsync(force: true);
            ads.Discard();
            if (!OwnsRemoveAds) await consent.RefreshAsync();
            Preload();
        }
        catch { /* Provider outages must never prevent play. */ }
        finally { operation.Release(); }
    }

    public void BeginAttempt(string attemptId, string puzzleId, bool referred = false)
    {
        Activity(false);
        store.Update(s =>
        {
            s.AttemptId = attemptId; s.PuzzleId = puzzleId;
            s.AttemptConfig = s.EffectiveConfig;
            if (s.UnconfirmedHintReward is { } reward)
            {
                if (!s.PendingRewards.Contains(reward)) s.PendingRewards.Add(reward);
                s.UnconfirmedHintReward = null;
            }
            s.AttemptAdFree = referred && !s.ReferredExemptionUsed && s.EffectiveConfig.FirstReferredAdFree;
            if (s.AttemptAdFree) s.ReferredExemptionUsed = true;
            return true;
        });
        visibleOffer = null;
        Track("puzzle_started");
        Preload();
    }

    public void Activity(bool playing)
    {
        var now = time.GetUtcNow();
        var newSession = store.Update(s =>
        {
            var expired = s.LastInteractionUtc == default || now - s.LastInteractionUtc >= TimeSpan.FromMinutes(30);
            if (expired)
            {
                s.SessionId = Guid.NewGuid().ToString("N"); s.SessionInterstitials = 0;
                // Cooldown survives session boundaries; only session limits reset.
                s.EffectiveConfig = s.CachedConfig;
            }
            else if (activeSince is { } start)
                s.ActiveSeconds += Math.Clamp((now - start).TotalSeconds, 0, 1800);
            s.LastInteractionUtc = now;
            return expired;
        });
        activeSince = playing ? now : null;
        if (newSession)
        {
            Track("session_start");
            if (store.Update(s => s.EffectiveConfig.ExperimentEnabled)) Track("experiment_exposure");
        }
        if (playing && IsEnabled && time.GetUtcNow() - lastConfigFetch >= TimeSpan.FromMinutes(1))
            _ = RefreshConfigurationAsync();
    }

    private async Task RefreshConfigurationAsync(bool force = false)
    {
        if (fetchingConfig || (!force && time.GetUtcNow() - lastConfigFetch < TimeSpan.FromMinutes(1))) return;
        fetchingConfig = true; lastConfigFetch = time.GetUtcNow();
        try
        {
            var config = await configuration.FetchAsync();
            if (config is not { IsValid: true }) return;
            store.Update(s =>
            {
                s.CachedConfig = config; s.InterstitialKilled = !config.InterstitialEnabled;
                s.RewardedKilled = !config.RewardedEnabled; return true;
            });
            if (!config.InterstitialEnabled || !config.RewardedEnabled) ads.Discard();
        }
        catch { /* Retain last known valid configuration. */ }
        finally { fetchingConfig = false; }
    }

    public void Suspend()
    {
        // Do not update last interaction while backgrounded repeatedly.
        if (activeSince is not null) Activity(false);
    }

    private HintSource Source(MonetizationState s)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        // Moving the device clock backwards cannot replenish today's allowance.
        if (today > s.DailyDate) { s.DailyDate = today; s.DailyHintsUsed = 0; }
        if (s.OwnsRemoveAds && s.DailyHintsUsed < MonetizationConfig.DailyHints) return HintSource.Daily;
        if (s.WelcomeHints > 0) return HintSource.Welcome;
        if (s.PendingRewards.Count > 0) return HintSource.Reward;
        if (s.OwnsRemoveAds || !CanAdvertise(s, AdFormat.Rewarded) || !CooldownPassed(s)) return HintSource.Unavailable;
        return HintSource.Advertisement;
    }

    public HintOffer GetHintOffer(bool hasUsefulHint)
    {
        var source = !IsEnabled ? HintSource.Welcome : store.Update(Source);
        if (!hasUsefulHint) source = HintSource.Unavailable;
        var label = source switch
        {
            HintSource.Daily => "Usar pista diaria incluida",
            HintSource.Welcome => "Usar pista gratuita",
            HintSource.Reward => "Usar pista ganada",
            HintSource.Advertisement => "Ver anuncio para obtener una pista",
            _ => "Pista no disponible ahora"
        };
        visibleOffer = new(source, label, hasUsefulHint ? "availability" : "no_useful_hint", Guid.NewGuid().ToString("N"));
        Track("ad_opportunity", ("format", "rewarded"), ("eligible", source == HintSource.Advertisement), ("reason", visibleOffer.Reason));
        return visibleOffer;
    }

    public async Task<bool> AcceptHintAsync(HintOffer offer)
    {
        if (!ReferenceEquals(visibleOffer, offer) || offer.Source == HintSource.Unavailable || !await operation.WaitAsync(0)) return false;
        visibleOffer = null;
        try
        {
            if (!IsEnabled) return true;
            if (offer.Source != HintSource.Advertisement) return store.Update(s => Source(s) is HintSource.Welcome or HintSource.Daily or HintSource.Reward);
            Track("reward_offer_accept", ("offer_id", offer.OfferId));
            if (!store.Update(s => CanAdvertise(s, AdFormat.Rewarded) && CooldownPassed(s)) || !ads.IsReady(AdFormat.Rewarded))
            { Preload(); return false; }
            await ads.ShowAsync(AdFormat.Rewarded, () => store.Update(s => CanAdvertise(s, AdFormat.Rewarded) && CooldownPassed(s)));
            return store.Update(s => s.PendingRewards.Count > 0);
        }
        catch { return false; }
        finally { operation.Release(); }
    }

    public bool ConsumeHint(string attemptId, Func<bool> deliver)
    {
        if (!IsEnabled) return deliver();
        var result = store.Update<(bool Granted, HintSource Source, string? Reward)>(s =>
        {
            if (s.AttemptId != attemptId) return (Granted: false, Source: HintSource.Unavailable, Reward: (string?)null);
            var source = Source(s);
            if (source is HintSource.Unavailable or HintSource.Advertisement || !deliver()) return (false, source, null);
            string? reward = null;
            if (source == HintSource.Daily) s.DailyHintsUsed++;
            if (source == HintSource.Welcome) s.WelcomeHints--;
            if (source == HintSource.Reward) { reward = s.PendingRewards[0]; s.PendingRewards.RemoveAt(0); s.UnconfirmedHintReward = reward; }
            return (true, source, reward);
        });
        if (result.Granted) Track("hint_granted", ("source", result.Source.ToString().ToLowerInvariant()), ("reward_id", result.Reward ?? ""));
        return result.Granted;
    }

    public void ConfirmHintDisplayed() => store.Update(s => { s.UnconfirmedHintReward = null; return true; });

    public async Task ClearPersonalDataAsync()
    {
        await operation.WaitAsync();
        try
        {
            ads.Discard(); activeSince = null; visibleOffer = null;
            store.Update(s =>
            {
                s.InstallationId = Guid.NewGuid().ToString("N"); s.SessionId = Guid.NewGuid().ToString("N");
                s.AttemptId = null; s.PuzzleId = null; s.LastCompletedAttempt = null; s.LastInteractionUtc = default;
                s.ActiveSeconds = 0; s.LastAdActiveSeconds = null; s.SessionInterstitials = 0; s.CompletedAttempts = 0;
                s.PendingRewards.Clear(); s.EarnedRewards.Clear(); s.Impressions.Clear(); s.UnconfirmedHintReward = null;
                s.AttemptConfig = null; s.AttemptAdFree = false;
                // Retain only the paid benefit and anonymous per-install allowances.
                s.EntitlementResolved = s.OwnsRemoveAds;
                return true;
            });
        }
        finally { operation.Release(); }
    }

    public async Task CompleteAttemptAsync(string attemptId, bool success)
    {
        Activity(false);
        if (!IsEnabled) return;
        var reason = store.Update(s =>
        {
            if (attemptId != s.AttemptId || s.LastCompletedAttempt == attemptId) return "duplicate";
            s.LastCompletedAttempt = attemptId;
            if (!success) return "not_completed";
            s.CompletedAttempts++;
            var c = s.AttemptConfig ?? s.EffectiveConfig;
            if (s.CompletedAttempts <= c.GraceCompleted || s.CompletedAttempts % c.EveryCompleted != 0) return "frequency";
            if (s.AttemptAdFree) return "referred";
            if (!CanAdvertise(s, AdFormat.Interstitial)) return "disabled";
            if (s.SessionInterstitials >= c.MaxPerSession) return "session_limit";
            if (s.ActiveSeconds < c.MinActiveSeconds || !CooldownPassed(s)) return "cooldown";
            return "eligible";
        });
        if (reason == "duplicate") return;
        Track(success ? "puzzle_completed" : "puzzle_failed");
        Track("ad_opportunity", ("format", "interstitial"), ("eligible", reason == "eligible"), ("reason", reason));
        if (reason != "eligible" || !ads.IsReady(AdFormat.Interstitial) || !await operation.WaitAsync(0)) return;
        try
        {
            await ads.ShowAsync(AdFormat.Interstitial, () => store.Update(s => CanAdvertise(s, AdFormat.Interstitial) && CooldownPassed(s)));
        }
        catch { /* The consumed opportunity is never shown late. */ }
        finally { operation.Release(); }
    }

    private bool CooldownPassed(MonetizationState s) => s.LastAdActiveSeconds is not { } last || s.ActiveSeconds - last >= (s.AttemptConfig ?? s.EffectiveConfig).MinActiveSeconds;
    private bool CanAdvertise(MonetizationState s, AdFormat format) => IsEnabled && s.EntitlementResolved && !s.OwnsRemoveAds && consent.CanRequestAds &&
        (format == AdFormat.Rewarded ? (s.AttemptConfig ?? s.EffectiveConfig).RewardedEnabled && !s.RewardedKilled :
            (s.AttemptConfig ?? s.EffectiveConfig).InterstitialEnabled && !s.InterstitialKilled && (!(s.AttemptConfig ?? s.EffectiveConfig).ExperimentEnabled || s.Variant == "moderate_interstitials"));

    private void Preload()
    {
        if (!IsEnabled) return;
        foreach (var format in new[] { AdFormat.Rewarded, AdFormat.Interstitial })
            if (store.Update(s => CanAdvertise(s, format)))
                try { ads.Preload(format, () => store.Update(s => CanAdvertise(s, format))); } catch { }
    }

    private void OnAdSignal(AdSignal signal)
    {
        if (signal.Name == "ad_impression")
            store.Update(s =>
            {
                if (!s.Impressions.Add(signal.InstanceId)) return false;
                s.LastAdActiveSeconds = s.ActiveSeconds;
                if (signal.Format == AdFormat.Interstitial) s.SessionInterstitials++;
                return true;
            });
        if (signal.Name == "reward_earned" && signal.RewardId is { } reward)
            store.Update(s => { if (s.EarnedRewards.Add(reward)) s.PendingRewards.Add(reward); return true; });
        // Canonical impression/revenue events belong to the Firebase/AdMob integration.
        Track(signal.Name == "ad_impression" ? "ad_impression_diagnostic" : signal.Name,
            ("ad_instance_id", signal.InstanceId), ("format", signal.Format.ToString().ToLowerInvariant()),
            ("ad_request_id", signal.InstanceId), ("placement_id", signal.Format == AdFormat.Rewarded ? "hint" : "level_end"),
            ("status", signal.ErrorCode is null ? "success" : "failed"),
            ("reward_id", signal.RewardId ?? ""), ("error_code", signal.ErrorCode ?? ""));
    }

    public async Task<StoreProduct?> GetProductAsync()
    {
        try { return IsEnabled ? await purchases.GetProductAsync() : null; } catch { return null; }
    }

    public async Task<PurchaseStatus> PurchaseAsync()
    {
        if (!IsEnabled || !await operation.WaitAsync(0)) return PurchaseStatus.Failed;
        Suspend();
        try
        {
            Track("purchase_started", ("product_id", MonetizationConfig.ProductId));
            var outcome = await purchases.PurchaseAsync();
            var status = outcome.Status;
            if (outcome.Proof is { } proof)
            {
                var entitlement = await gateway.VerifyAsync(proof);
                if (entitlement.Verified && entitlement.Owned)
                { ApplyEntitlement(entitlement); await purchases.FinishAsync(proof); status = PurchaseStatus.Verified; }
                else status = PurchaseStatus.Pending;
            }
            Track("purchase_result", ("status", status.ToString().ToLowerInvariant()));
            return status;
        }
        catch { Track("purchase_result", ("status", "failed")); return PurchaseStatus.Failed; }
        finally { operation.Release(); }
    }

    public async Task<bool> RestoreAsync()
    {
        if (!IsEnabled || !await operation.WaitAsync(0)) return false;
        try { return await ReconcileAsync(userInitiated: true); }
        finally { operation.Release(); }
    }

    private async Task<bool> ReconcileAsync(bool userInitiated = false)
    {
        try
        {
            foreach (var proof in await purchases.RestoreAsync(userInitiated))
            {
                var restored = await gateway.VerifyAsync(proof);
                if (!restored.Verified) return false;
                ApplyEntitlement(restored);
                if (restored.Owned) await purchases.FinishAsync(proof);
            }
            var entitlement = await gateway.GetAsync();
            ApplyEntitlement(entitlement);
            return entitlement.Verified;
        }
        catch { return false; }
    }

    private async Task ReconcileUpdateAsync()
    {
        if (!IsEnabled || !await operation.WaitAsync(0)) return;
        try { await ReconcileAsync(); }
        finally { operation.Release(); }
    }

    private void ApplyEntitlement(Entitlement entitlement)
    {
        if (!entitlement.Verified) return;
        store.Update(s => { s.OwnsRemoveAds = entitlement.Owned; s.EntitlementResolved = true; return true; });
        if (entitlement.Owned) ads.Discard();
        Track("entitlement_changed", ("status", entitlement.Owned ? "verified" : "not_owned"), ("source", entitlement.Source));
    }

    public async Task ShowPrivacyOptionsAsync()
    {
        Suspend(); ads.Discard();
        try { await consent.ShowPrivacyOptionsAsync(); } finally { Preload(); }
    }

    private void Track(string name, params (string Key, object Value)[] fields)
    {
        try
        {
            var data = store.Update(s => new Dictionary<string, object>
            {
                ["event_id"] = Guid.NewGuid().ToString("N"), ["event_schema_version"] = 1,
                ["session_id"] = s.SessionId, ["attempt_id"] = s.AttemptId ?? "",
                ["puzzle_id"] = s.PuzzleId ?? "", ["config_version"] = (s.AttemptConfig ?? s.EffectiveConfig).Version,
                ["active_seconds"] = s.ActiveSeconds,
                ["variant"] = s.EffectiveConfig.ExperimentEnabled ? s.Variant : "baseline"
            });
            foreach (var field in fields) data[field.Key] = field.Value;
            telemetry.Track(name, data);
        }
        catch { /* Analytics is never a dependency of gameplay. */ }
    }
}
