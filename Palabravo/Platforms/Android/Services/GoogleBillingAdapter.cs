using Android.BillingClient.Api;
using Palabravo.Core.Monetization;

namespace Palabravo.Services.Monetization;

public sealed class GoogleBillingAdapter : Java.Lang.Object, IPurchaseAdapter, IPurchasesUpdatedListener
{
    private readonly BillingClient client;
    private TaskCompletionSource<PurchaseOutcome>? purchase;
    private ProductDetails? product;
    public event Action? Changed;
    public GoogleBillingAdapter()
    {
        client = BillingClient.NewBuilder(Platform.AppContext).SetListener(this)
            .EnablePendingPurchases(PendingPurchasesParams.NewBuilder().EnableOneTimeProducts().Build())
            .EnableAutoServiceReconnection().Build();
    }

    private async Task ConnectAsync()
    {
        if (client.IsReady) return;
        var listener = new Connection();
        client.StartConnection(listener);
        await listener.Completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
    }

    public async Task<StoreProduct?> GetProductAsync()
    {
        await ConnectAsync();
        var listener = new Products();
        var item = QueryProductDetailsParams.Product.NewBuilder().SetProductId(MonetizationConfig.ProductId).SetProductType(BillingClient.ProductType.Inapp).Build();
        client.QueryProductDetails(QueryProductDetailsParams.NewBuilder().SetProductList(new[] { item }).Build(), listener);
        product = (await listener.Completion.Task.WaitAsync(TimeSpan.FromSeconds(15))).FirstOrDefault();
        var offer = product?.GetOneTimePurchaseOfferDetails();
        return offer is null ? null : new(MonetizationConfig.ProductId, offer.FormattedPrice, offer.PriceCurrencyCode, offer.PriceAmountMicros / 1_000_000m);
    }

    public Task<PurchaseOutcome> PurchaseAsync() => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        if (purchase is not null || await GetProductAsync() is null) return new PurchaseOutcome(PurchaseStatus.Failed);
        purchase = new(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var details = BillingFlowParams.ProductDetailsParams.NewBuilder().SetProductDetails(product!).Build();
            var result = client.LaunchBillingFlow(Platform.CurrentActivity!, BillingFlowParams.NewBuilder().SetProductDetailsParamsList(new[] { details }).Build());
            if (result.ResponseCode != BillingResponseCode.Ok) return new PurchaseOutcome(PurchaseStatus.Failed);
            return await purchase.Task;
        }
        finally { purchase = null; }
    });

    public void OnPurchasesUpdated(BillingResult result, IList<Purchase>? list)
    {
        if (result.ResponseCode == BillingResponseCode.UserCancelled) { purchase?.TrySetResult(new(PurchaseStatus.Cancelled)); return; }
        if (result.ResponseCode != BillingResponseCode.Ok) { purchase?.TrySetResult(new(PurchaseStatus.Failed)); return; }
        var item = list?.FirstOrDefault(x => x.Products.Contains(MonetizationConfig.ProductId));
        if (item is null) { purchase?.TrySetResult(new(PurchaseStatus.Failed)); return; }
        purchase?.TrySetResult(item.PurchaseState == PurchaseState.Purchased
            ? new(PurchaseStatus.Pending, Proof(item)) : new(PurchaseStatus.Pending));
        Changed?.Invoke();
    }

    public async Task<IReadOnlyList<PurchaseProof>> RestoreAsync(bool userInitiated = false)
    {
        await ConnectAsync();
        var listener = new OwnedProducts();
        client.QueryPurchases(QueryPurchasesParams.NewBuilder().SetProductType(BillingClient.ProductType.Inapp).Build(), listener);
        return (await listener.Completion.Task.WaitAsync(TimeSpan.FromSeconds(15)))
            .Where(x => x.PurchaseState == PurchaseState.Purchased && x.Products.Contains(MonetizationConfig.ProductId)).Select(Proof).ToList();
    }
    private static PurchaseProof Proof(Purchase purchase) => new("android", purchase.OrderId ?? "", purchase.PurchaseToken);
    // Acknowledgement is performed durably by the backend after storing the grant.
    public Task FinishAsync(PurchaseProof proof) => Task.CompletedTask;
    private static void Ensure(BillingResult result) { if (result.ResponseCode != BillingResponseCode.Ok) throw new InvalidOperationException("Tienda no disponible."); }
    private sealed class Connection : Java.Lang.Object, IBillingClientStateListener
    {
        public TaskCompletionSource Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void OnBillingSetupFinished(BillingResult result)
        { if (result.ResponseCode == BillingResponseCode.Ok) Completion.TrySetResult(); else Completion.TrySetException(new IOException("Tienda no disponible.")); }
        public void OnBillingServiceDisconnected() => Completion.TrySetException(new IOException("Tienda desconectada."));
    }
    private sealed class Products : Java.Lang.Object, IProductDetailsResponseListener
    {
        public TaskCompletionSource<IList<ProductDetails>> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void OnProductDetailsResponse(BillingResult result, QueryProductDetailsResult products)
        { try { Ensure(result); Completion.TrySetResult(products.ProductDetailsList); } catch (Exception e) { Completion.TrySetException(e); } }
    }
    private sealed class OwnedProducts : Java.Lang.Object, IPurchasesResponseListener
    {
        public TaskCompletionSource<IList<Purchase>> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void OnQueryPurchasesResponse(BillingResult result, IList<Purchase> purchases)
        { try { Ensure(result); Completion.TrySetResult(purchases); } catch (Exception e) { Completion.TrySetException(e); } }
    }
}
