using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Palabravo
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ScreenOrientation = ScreenOrientation.Portrait, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter(new[] { Android.Content.Intent.ActionView }, Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable }, DataScheme = "https", DataHost = "palabravo.app", DataPathPrefix = "/reto/", AutoVerify = true)]
    [IntentFilter(new[] { Android.Content.Intent.ActionView }, Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable }, DataScheme = "https", DataHost = "palabravo.app", DataPathPrefix = "/challenge/", AutoVerify = true)]
    [IntentFilter(new[] { Android.Content.Intent.ActionView }, Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable }, DataScheme = "https", DataHost = "palabravo.app", DataPath = "/semanal", AutoVerify = true)]
    [IntentFilter(new[] { Android.Content.Intent.ActionView }, Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable }, DataScheme = "palabravo", DataHost = "semanal")]
    [IntentFilter(new[] { Android.Content.Intent.ActionView }, Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable }, DataScheme = "palabravo", DataHost = "challenge")]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Receive(Intent);
        }
        protected override void OnNewIntent(Android.Content.Intent? intent)
        {
            base.OnNewIntent(intent);
            Receive(intent);
        }
        private static void Receive(Android.Content.Intent? intent)
        {
            if (Uri.TryCreate(intent?.DataString, UriKind.Absolute, out var uri))
                IPlatformApplication.Current!.Services.GetRequiredService<Services.ReferralService>().Receive(uri);
        }
    }
}
