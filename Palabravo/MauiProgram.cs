using Microsoft.Extensions.Logging;
using Palabravo.Core.Services;
using Palabravo.Services;
using Palabravo.ViewModels;
using Palabravo.Views;
using Plugin.Maui.Audio;

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
        builder.Services.AddSingleton<IPuzzleRepository, JsonPuzzleRepository>();
        builder.Services.AddSingleton<IProgressStore, PreferencesProgressStore>();
        builder.Services.AddSingleton<ProgressService>();
        builder.Services.AddSingleton<GameCoordinator>();
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
