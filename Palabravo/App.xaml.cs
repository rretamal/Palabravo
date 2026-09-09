namespace Palabravo;

public partial class App : Application
{
    private readonly AppShell _shell;
    private readonly Core.Monetization.IMonetizationService _monetization;
    private readonly Core.Services.GameplayActivity _activity;
    private readonly Services.ReferralService _referrals;

    public App(AppShell shell, Core.Monetization.IMonetizationService monetization, Core.Services.GameplayActivity activity, Services.ReferralService referrals)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        _shell = shell;
        _monetization = monetization;
        _activity = activity;
        _referrals = referrals;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_shell);
        window.Deactivated += (_, _) =>
        {
            _activity.SetForeground(false);
        };
        window.Activated += async (_, _) =>
        {
            _activity.SetForeground(true);
            // Background store/config requests do not pause gameplay. The
            // consent adapter owns the pause around its presentation instead.
            await _monetization.InitializeAsync();
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
