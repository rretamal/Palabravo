using Microsoft.Extensions.DependencyInjection;
using Palabravo.Views;

namespace Palabravo;

public partial class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        InitializeComponent();
        HomeContent.ContentTemplate = new DataTemplate(() => services.GetRequiredService<HomePage>());
        ChallengesContent.ContentTemplate = new DataTemplate(() => services.GetRequiredService<ChallengesPage>());
        RankingContent.ContentTemplate = new DataTemplate(() => services.GetRequiredService<RankingPage>());
        ProfileContent.ContentTemplate = new DataTemplate(() => services.GetRequiredService<ProfilePage>());

        Routing.RegisterRoute(nameof(GamePage), typeof(GamePage));
        Routing.RegisterRoute(nameof(ResultPage), typeof(ResultPage));
        Routing.RegisterRoute(nameof(TutorialPage), typeof(TutorialPage));
    }
}
