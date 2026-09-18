namespace Palabravo;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly Core.Monetization.IMonetizationService _monetization;
    private readonly Core.Services.GameplayActivity _activity;
    private readonly Services.ReferralService _referrals;
    private readonly Services.EngagementNotificationService _notifications;

    public App(AppShell shell, Core.Monetization.IMonetizationService monetization, Core.Services.GameplayActivity activity,
        Services.ReferralService referrals, Services.EngagementNotificationService notifications)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        _shell = shell;
        _monetization = monetization;
        _activity = activity;
        _referrals = referrals;
        _notifications = notifications;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_shell);
        window.Deactivated += async (_, _) =>
        {
            _activity.SetForeground(false);
            await _notifications.OnDeactivatedAsync();
        };
        window.Activated += async (_, _) =>
        {
            _activity.SetForeground(true);
            // Background store/config requests do not pause gameplay. The
            // consent adapter owns the pause around its presentation instead.
            await _monetization.InitializeAsync();
            await _notifications.OnActivatedAsync();
            await _notifications.PromptOnFirstUseAsync(() => _shell.DisplayAlertAsync(
                "¿Activar notificaciones?",
                "Te avisaremos cuando haya retos nuevos y, después de dos días sin jugar, te recordaremos que puedes seguir avanzando. Puedes cambiar cada aviso desde Perfil.",
                "Activar", "Ahora no"));
            _activity.Touch();
            await _referrals.OpenPendingAsync();
        };
        return window;
    }

    protected override void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
        _referrals.Receive(uri);
    }
}
