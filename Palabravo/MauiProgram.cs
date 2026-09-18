using Microsoft.Extensions.Logging;
using Palabravo.Core.Services;
using Palabravo.Services;
using Palabravo.ViewModels;
using Palabravo.Views;
using Plugin.Maui.Audio;
using Palabravo.Core.Monetization;
using Palabravo.Services.Monetization;
using Microsoft.Maui.LifecycleEvents;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models.AndroidOption;
#if ANDROID || IOS
using Plugin.AdMob;
using Plugin.AdMob.Configuration;
using Plugin.Firebase.CloudMessaging;
#endif

namespace Palabravo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseLocalNotification(config =>
            {
#if ANDROID
                config.AddAndroid(android => android.AddChannel(new AndroidNotificationChannelRequest
                {
                    Id = EngagementNotificationService.UpdatesChannelId,
                    Name = "Recordatorios y nuevos retos",
                    Description = "Avisos opcionales para continuar jugando y descubrir retos nuevos."
                }));
#endif
            })
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
            androidDefaultBannerAdUnitId: monetization.AndroidBannerId,
            androidDefaultInterstitialAdUnitId: monetization.AndroidInterstitialId,
            androidDefaultRewardedAdUnitId: monetization.AndroidRewardedId,
            iosDefaultBannerAdUnitId: monetization.IosBannerId,
            iosDefaultInterstitialAdUnitId: monetization.IosInterstitialId,
            iosDefaultRewardedAdUnitId: monetization.IosRewardedId);
#if DEBUG
        // Development builds must never generate traffic against production ad units.
        AdConfig.UseTestAdUnitIds = false;
#else
        AdConfig.UseTestAdUnitIds = monetization.TestAds;
#endif
        builder.Services.AddSingleton<IAdAdapter, AdMobAdapter>();
        builder.Services.AddSingleton<IConsentAdapter, AdMobConsentAdapter>();
        builder.Services.AddSingleton<FirebaseAdapter>();
        builder.Services.AddSingleton<IMonetizationConfiguration>(s => s.GetRequiredService<FirebaseAdapter>());
        builder.Services.AddSingleton<IMonetizationTelemetry>(s => s.GetRequiredService<FirebaseAdapter>());
#if ANDROID
        builder.Services.AddSingleton<IPurchaseAdapter, GoogleBillingAdapter>();
        builder.ConfigureLifecycleEvents(events => events.AddAndroid(android => android.OnCreate((activity, _) =>
        {
            try
            {
                Plugin.Firebase.Core.Platforms.Android.CrossFirebase.Initialize(activity, () => Platform.CurrentActivity!);
                Plugin.Firebase.Analytics.FirebaseAnalyticsImplementation.Initialize(activity);
                FirebaseCloudMessagingImplementation.ChannelId = EngagementNotificationService.UpdatesChannelId;
                FirebaseAdapter.Ready = true;
                FirebaseAdapter.SetCollection(Preferences.Default.Get("analytics_consent", false));
            }
            catch { FirebaseAdapter.Ready = false; }
        })));
#else
        builder.Services.AddSingleton<IPurchaseAdapter, StoreKitAdapter>();
        builder.ConfigureLifecycleEvents(events => events.AddiOS(ios => ios.FinishedLaunching((_, _) =>
        {
            if (Foundation.NSBundle.MainBundle.PathForResource("GoogleService-Info", "plist") is not null)
            {
                Plugin.Firebase.Core.Platforms.iOS.CrossFirebase.Initialize();
                FirebaseCloudMessagingImplementation.Initialize();
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
        builder.Services.AddSingleton<WeeklyChallengeService>();
        builder.Services.AddSingleton<PathRankingService>();
        builder.Services.AddSingleton<GameCoordinator>();
        builder.Services.AddSingleton<ReferralService>();
        builder.Services.AddSingleton<EngagementNotificationService>();
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
