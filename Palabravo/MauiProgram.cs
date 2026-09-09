using Microsoft.Extensions.Logging;
using Palabravo.Core.Services;
using Palabravo.Services;
using Palabravo.ViewModels;
using Palabravo.Views;
using Plugin.Maui.Audio;
using Palabravo.Core.Monetization;
using Palabravo.Services.Monetization;
using Microsoft.Maui.LifecycleEvents;
#if ANDROID || IOS
using Plugin.AdMob;
using Plugin.AdMob.Configuration;
#endif

namespace Palabravo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .AddAudio()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("Lora-Variable.ttf", "LoraSemibold");
            });

        builder.Services.AddSingleton<IClock, SystemClock>();
        var monetization = MonetizationSettings.Current;
        builder.Services.AddSingleton<IMonetizationStore>(_ => new AtomicMonetizationStore(Path.Combine(FileSystem.AppDataDirectory, "monetization-v1.json")));
        builder.Services.AddSingleton<IEntitlementGateway, EntitlementGateway>();
#if ANDROID || IOS
        builder.UseAdMob(automaticallyAskForConsent: false,
            androidDefaultInterstitialAdUnitId: monetization.AndroidInterstitialId,
            androidDefaultRewardedAdUnitId: monetization.AndroidRewardedId,
            iosDefaultInterstitialAdUnitId: monetization.IosInterstitialId,
            iosDefaultRewardedAdUnitId: monetization.IosRewardedId);
        AdConfig.UseTestAdUnitIds = monetization.TestAds;
        builder.Services.AddSingleton<IAdAdapter, AdMobAdapter>();
        builder.Services.AddSingleton<IConsentAdapter, AdMobConsentAdapter>();
        builder.Services.AddSingleton<FirebaseAdapter>();
        builder.Services.AddSingleton<IMonetizationConfiguration>(s => s.GetRequiredService<FirebaseAdapter>());
        builder.Services.AddSingleton<IMonetizationTelemetry>(s => s.GetRequiredService<FirebaseAdapter>());
#if ANDROID
        builder.Services.AddSingleton<IPurchaseAdapter, GoogleBillingAdapter>();
        builder.ConfigureLifecycleEvents(events => events.AddAndroid(android => android.OnCreate((activity, _) =>
        {
            if (!monetization.Enabled) return;
            try
            {
                Plugin.Firebase.Core.Platforms.Android.CrossFirebase.Initialize(activity, () => Platform.CurrentActivity!);
                Plugin.Firebase.Analytics.FirebaseAnalyticsImplementation.Initialize(activity);
                FirebaseAdapter.Ready = true;
                FirebaseAdapter.SetCollection(Preferences.Default.Get("analytics_consent", false));
            }
            catch { FirebaseAdapter.Ready = false; }
        })));
#else
        builder.Services.AddSingleton<IPurchaseAdapter, StoreKitAdapter>();
        builder.ConfigureLifecycleEvents(events => events.AddiOS(ios => ios.FinishedLaunching((_, _) =>
        {
            if (monetization.Enabled && Foundation.NSBundle.MainBundle.PathForResource("GoogleService-Info", "plist") is not null)
            {
                Plugin.Firebase.Core.Platforms.iOS.CrossFirebase.Initialize();
                FirebaseAdapter.Ready = true;
                FirebaseAdapter.SetCollection(Preferences.Default.Get("analytics_consent", false));
            }
            return true;
        })));
#endif
#else
        builder.Services.AddSingleton<UnavailableProviders>();
        builder.Services.AddSingleton<IAdAdapter>(s => s.GetRequiredService<UnavailableProviders>());
        builder.Services.AddSingleton<IConsentAdapter>(s => s.GetRequiredService<UnavailableProviders>());
        builder.Services.AddSingleton<IPurchaseAdapter>(s => s.GetRequiredService<UnavailableProviders>());
        builder.Services.AddSingleton<IMonetizationConfiguration>(s => s.GetRequiredService<UnavailableProviders>());
        builder.Services.AddSingleton<IMonetizationTelemetry>(s => s.GetRequiredService<UnavailableProviders>());
#endif
        builder.Services.AddSingleton<GameplayActivity>();
        builder.Services.AddSingleton<IMonetizationService>(s =>
        {
            var monetizationService = new MonetizationService(
            s.GetRequiredService<IMonetizationStore>(), s.GetRequiredService<IAdAdapter>(),
            s.GetRequiredService<IConsentAdapter>(), s.GetRequiredService<IPurchaseAdapter>(),
            s.GetRequiredService<IEntitlementGateway>(), s.GetRequiredService<IMonetizationConfiguration>(),
            s.GetRequiredService<IMonetizationTelemetry>(), enabled: monetization.Enabled &&
                (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS));
            s.GetRequiredService<GameplayActivity>().Changed += monetizationService.Activity;
            return monetizationService;
        });
        builder.Services.AddSingleton<MonetizationOfferPresenter>();
        builder.Services.AddSingleton<IPuzzleRepository, JsonPuzzleRepository>();
        builder.Services.AddSingleton<IProgressStore, PreferencesProgressStore>();
        builder.Services.AddSingleton<ProgressService>();
        builder.Services.AddSingleton<GameCoordinator>();
        builder.Services.AddSingleton<ReferralService>();
        builder.Services.AddSingleton<CelebrationEffectsService>();
        builder.Services.AddSingleton<IAccountDeletionGateway, AccountDeletionGateway>();
        builder.Services.AddSingleton<PlayFabLeaderboardService>();
        builder.Services.AddSingleton<ILeaderboardService>(services => services.GetRequiredService<PlayFabLeaderboardService>());
        builder.Services.AddSingleton<IPlayerAccountService>(services => services.GetRequiredService<PlayFabLeaderboardService>());
#if ANDROID
        builder.Services.AddSingleton<IGooglePlayGamesAuthService, GooglePlayGamesAuthService>();
#else
        builder.Services.AddSingleton<IGooglePlayGamesAuthService, UnsupportedGooglePlayGamesAuthService>();
#endif
        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<ChallengesViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<RankingViewModel>();
        builder.Services.AddTransient<GameViewModel>();
        builder.Services.AddTransient<ResultViewModel>();

        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ChallengesPage>();
        builder.Services.AddTransient<RankingPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<GamePage>();
        builder.Services.AddTransient<ResultPage>();
        builder.Services.AddTransient<TutorialPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
