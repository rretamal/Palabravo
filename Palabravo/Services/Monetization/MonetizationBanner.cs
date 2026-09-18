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
            HorizontalOptions = LayoutOptions.Center
        };
        banner.OnAdLoaded += (_, _) => host.IsVisible = monetization.CanShowBanner;
        banner.OnAdFailedToLoad += (_, _) =>
        {
            host.IsVisible = false;
            host.Content = null;
        };
        host.Content = banner;
#endif
    }
}
