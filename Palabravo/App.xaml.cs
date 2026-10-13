namespace Palabravo;

public partial class App : Application
{
    private const string GooglePromptPreference = "google_ranking_first_use_prompted_v1";
    private bool googlePromptRunning;
    private bool activationRunning;
    private readonly Services.IPlayerAccountService _account;
    private readonly AppShell _shell;
    private readonly Core.Monetization.IMonetizationService _monetization;
    private readonly Core.Services.GameplayActivity _activity;
    private readonly Services.ReferralService _referrals;
    private readonly Services.EngagementNotificationService _notifications;

    public App(AppShell shell, Core.Monetization.IMonetizationService monetization, Core.Services.GameplayActivity activity,
        Services.ReferralService referrals, Services.EngagementNotificationService notifications,
        Services.IPlayerAccountService account)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        _shell = shell;
        _monetization = monetization;
        _activity = activity;
        _referrals = referrals;
        _notifications = notifications;
        _account = account;
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
            // Google sign-in can reactivate the window; finish the current flow
            // before offering another first-use dialog.
            if (activationRunning) return;
            activationRunning = true;
            try
            {
                // Background store/config requests do not pause gameplay. The
                // consent adapter owns the pause around its presentation instead.
                await _monetization.InitializeAsync();
                await _notifications.OnActivatedAsync();
                await PromptGoogleOnFirstUseAsync();
                await _notifications.PromptOnFirstUseAsync(() => _shell.DisplayAlertAsync(
                    "¿Activar notificaciones?",
                    "Te avisaremos cuando haya retos nuevos y, después de dos días sin jugar, te recordaremos que puedes seguir avanzando. Puedes cambiar cada aviso desde Perfil.",
                    "Activar", "Ahora no"));
                _activity.Touch();
                await _referrals.OpenPendingAsync();
            }
            finally { activationRunning = false; }
        };
        return window;
    }

    private async Task PromptGoogleOnFirstUseAsync()
    {
        var account = _account.GetSnapshot();
        if (!account.CanUseGooglePlayGames || account.IsGoogleLinked || googlePromptRunning ||
            Preferences.Default.Get(GooglePromptPreference, false)) return;

        googlePromptRunning = true;
        try
        {
            var connect = await _shell.DisplayAlertAsync("¿Usar Google para el ranking?",
                "Vincula tu cuenta con Google Play Games para conservar tu identidad en el ranking. También puedes jugar y participar como invitado, y vincular tu cuenta más tarde desde Inicio, Ranking o Perfil.",
                "Continuar con Google", "Seguir como invitado");
            Preferences.Default.Set(GooglePromptPreference, true);
            if (!connect) return;

            var result = await _account.ContinueWithGoogleAsync();
            if (_shell.CurrentPage?.BindingContext is ViewModels.HomeViewModel home)
                home.RefreshAccount();
            await _shell.DisplayAlertAsync(result.IsSuccess ? "Cuenta vinculada" : "No pudimos vincular tu cuenta",
                result.Message, "Cerrar");
        }
        finally { googlePromptRunning = false; }
    }

    protected override void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
        _referrals.Receive(uri);
    }
}
