namespace Palabravo.Core.Monetization;

public enum AdFormat { Rewarded, Interstitial }
public enum HintSource { Unavailable, Welcome, Daily, Reward, Advertisement }
public enum PurchaseStatus { Pending, Cancelled, Failed, Verified }
public sealed record HintOffer(HintSource Source, string Label, string Reason, string OfferId);
public sealed record StoreProduct(string Id, string Price, string Currency, decimal Amount);
public sealed record PurchaseProof(string Platform, string TransactionId, string Proof);
public sealed record PurchaseOutcome(PurchaseStatus Status, PurchaseProof? Proof = null);
public sealed record Entitlement(bool Verified, bool Owned, string Source);

public interface IMonetizationStore
{
    // The callback and the durable write form one serialized transaction.
    T Update<T>(Func<MonetizationState, T> update);
}

public interface IAdAdapter
{
    event Action<AdSignal>? Signal;
    bool IsReady(AdFormat format);
    void Preload(AdFormat format, Func<bool> isAllowed);
    Task ShowAsync(AdFormat format, Func<bool> isAllowed);
    void Discard();
}

public sealed record AdSignal(string Name, AdFormat Format, string InstanceId,
    string? ErrorCode = null, string? RewardId = null);

public interface IConsentAdapter
{
    bool CanRequestAds { get; }
    Task RefreshAsync();
    Task ShowPrivacyOptionsAsync();
}

public interface IPurchaseAdapter
{
    event Action? Changed;
    Task<StoreProduct?> GetProductAsync();
    Task<PurchaseOutcome> PurchaseAsync();
    Task<IReadOnlyList<PurchaseProof>> RestoreAsync(bool userInitiated = false);
    Task FinishAsync(PurchaseProof proof);
}

public interface IEntitlementGateway
{
    Task<Entitlement> VerifyAsync(PurchaseProof proof);
    Task<Entitlement> GetAsync();
}

public interface IMonetizationConfiguration
{
    Task<MonetizationConfig?> FetchAsync();
}

public interface IMonetizationTelemetry
{
    void Track(string name, IReadOnlyDictionary<string, object> parameters);
}

public interface IMonetizationService
{
    bool IsEnabled { get; }
    bool OwnsRemoveAds { get; }
    bool CanShowBanner { get; }
    Task InitializeAsync();
    void BeginAttempt(string attemptId, string puzzleId, bool referred = false);
    void Activity(bool playing);
    void Suspend();
    HintOffer GetHintOffer(bool hasUsefulHint);
    Task<bool> AcceptHintAsync(HintOffer offer);
    bool ConsumeHint(string attemptId, Func<bool> deliver);
    void ConfirmHintDisplayed();
    bool RetryNeedsAd(string puzzleId);
    Task<bool> AuthorizeRetryAsync(string puzzleId);
    Task ClearPersonalDataAsync();
    Task CompleteAttemptAsync(string attemptId, bool success);
    Task<StoreProduct?> GetProductAsync();
    Task<PurchaseStatus> PurchaseAsync();
    Task<bool> RestoreAsync();
    Task ShowPrivacyOptionsAsync();
}
