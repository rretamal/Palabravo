using Palabravo.Core.Monetization;

namespace Palabravo.Services.Monetization;

public sealed class MonetizationOfferPresenter(IMonetizationService monetization, IMonetizationTelemetry telemetry)
{
    public async Task ShowAsync()
    {
        if (monetization.OwnsRemoveAds)
        {
            await Shell.Current.DisplayAlertAsync("Palabravo sin anuncios", "Tu compra está activa. Incluye tres pistas diarias no acumulables, con un máximo de dos por intento. El cupo se renueva a las 00:00 UTC.", "Cerrar");
            return;
        }
        var product = await monetization.GetProductAsync();
        if (product is null)
        {
            await Shell.Current.DisplayAlertAsync("Tienda no disponible", "No pudimos consultar el precio. Puedes seguir jugando e intentarlo después.", "Cerrar");
            return;
        }
        var offer = Shell.Current.DisplayAlertAsync("Palabravo sin anuncios",
            $"Compra única de {product.Price}. Elimina todos los anuncios e incluye tres pistas diarias no acumulables (máximo dos por intento), renovadas a las 00:00 UTC. Restaurable dentro de esta tienda. No incluye futuros packs de pago.",
            $"Comprar · {product.Price}", "Ahora no");
        telemetry.Track("purchase_offer_view", new Dictionary<string, object> { ["product_id"] = product.Id, ["currency"] = product.Currency, ["price"] = (double)product.Amount, ["placement_id"] = "remove_ads_offer" });
        if (!await offer) return;
        var status = await monetization.PurchaseAsync();
        if (status == PurchaseStatus.Cancelled) return;
        await Shell.Current.DisplayAlertAsync("Palabravo sin anuncios", status switch
        {
            PurchaseStatus.Verified => "Compra confirmada. Ya tienes Palabravo sin anuncios.",
            PurchaseStatus.Pending => "La compra está pendiente de confirmación. Puedes seguir jugando.",
            _ => "No pudimos confirmar la compra. Puedes restaurarla o reintentar después."
        }, "Cerrar");
    }
}
