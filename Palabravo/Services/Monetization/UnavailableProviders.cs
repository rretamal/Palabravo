using Palabravo.Core.Monetization;

namespace Palabravo.Services.Monetization;

public sealed class UnavailableProviders : IAdAdapter, IConsentAdapter, IPurchaseAdapter,
    IMonetizationConfiguration, IMonetizationTelemetry
{
    public event Action<AdSignal>? Signal { add { } remove { } }
    public event Action? Changed { add { } remove { } }
    public bool CanRequestAds => false;
    public bool IsReady(AdFormat format) => false;
    public void Preload(AdFormat format, Func<bool> isAllowed) { }
    public Task ShowAsync(AdFormat format, Func<bool> isAllowed) => Task.CompletedTask;
    public void Discard() { }
    public Task RefreshAsync() => Task.CompletedTask;
    public Task ShowPrivacyOptionsAsync() => Task.CompletedTask;
    public Task<StoreProduct?> GetProductAsync() => Task.FromResult<StoreProduct?>(null);
    public Task<PurchaseOutcome> PurchaseAsync() => Task.FromResult(new PurchaseOutcome(PurchaseStatus.Failed));
    public Task<IReadOnlyList<PurchaseProof>> RestoreAsync(bool userInitiated = false) => throw new PlatformNotSupportedException();
    public Task FinishAsync(PurchaseProof proof) => Task.CompletedTask;
    public Task<MonetizationConfig?> FetchAsync() => Task.FromResult<MonetizationConfig?>(null);
    public void Track(string name, IReadOnlyDictionary<string, object> parameters) { }
}
