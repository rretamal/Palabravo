using Palabravo.Core.Monetization;

#if ANDROID || IOS
using Plugin.AdMob;
#endif

namespace Palabravo.Services.Monetization;

public static class MonetizationBanner
{
    public static void Attach(ContentView host, IMonetizationService monetization)
    {
        host.IsVisible = false;
#if ANDROID || IOS
        if (!monetization.CanShowBanner)
            return;

        if (host.Content is BannerAd existing)
        {
            host.IsVisible = existing.IsLoaded;
            return;
        }

        var banner = new BannerAd
        {
            AdSize = AdSize.Banner,
            Opacity = 0,
            HorizontalOptions = LayoutOptions.Center
        };
        banner.OnAdLoaded += (_, _) =>
        {
            banner.Opacity = 1;
            host.IsVisible = monetization.CanShowBanner;
        };
        banner.OnAdFailedToLoad += (_, _) =>
        {
            host.IsVisible = false;
            host.Content = null;
        };
        // MAUI must attach the native handler for AdMob to request the banner.
        // An invisible host prevents that attachment, so it can never load.
        host.IsVisible = true;
        host.Content = banner;
#endif
    }
}
