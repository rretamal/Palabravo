using Foundation;

namespace Palabravo
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
        public override bool ContinueUserActivity(UIKit.UIApplication application, NSUserActivity userActivity, UIKit.UIApplicationRestorationHandler completionHandler)
        {
            if (Uri.TryCreate(userActivity.WebPageUrl?.AbsoluteString, UriKind.Absolute, out var uri))
            {
                IPlatformApplication.Current!.Services.GetRequiredService<Services.ReferralService>().Receive(uri);
                return Core.Services.ReferralLink.Parse(uri) is not null;
            }
            return false;
        }
    }
}
