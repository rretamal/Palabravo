using Foundation;

namespace Palabravo
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
        public override bool ContinueUserActivity(UIKit.UIApplication application, NSUserActivity userActivity, UIKit.UIApplicationRestorationHandler completionHandler)
        {
            return Receive(userActivity.WebPageUrl?.AbsoluteString);
        }

        public override bool OpenUrl(UIKit.UIApplication application, NSUrl url, NSDictionary options) =>
            Receive(url.AbsoluteString);

        private static bool Receive(string? value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || uri.Scheme != "palabravo" && (uri.Scheme != "https" || uri.Host != "palabravo.app")) return false;
            IPlatformApplication.Current!.Services.GetRequiredService<Services.ReferralService>().Receive(uri);
            return true;
        }
    }
}
