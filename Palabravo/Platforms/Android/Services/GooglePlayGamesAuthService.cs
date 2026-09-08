using Android.Gms.Extensions;
using Android.Gms.Games;

namespace Palabravo.Services;

public sealed class GooglePlayGamesAuthService : IGooglePlayGamesAuthService
{
    private const string WebClientId = "748874878083-bnm215ir5oqbfga1seodatc9352qijk4.apps.googleusercontent.com";

    public bool IsSupported => true;

    public async Task<string> RequestServerAuthCodeAsync(CancellationToken cancellationToken = default)
    {
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("No hay una actividad Android disponible.");
        var client = PlayGames.GetGamesSignInClient(activity);

        var authentication = await client.IsAuthenticated().AsAsync<AuthenticationResult>();
        cancellationToken.ThrowIfCancellationRequested();
        if (!authentication.IsAuthenticated)
        {
            authentication = await client.SignIn().AsAsync<AuthenticationResult>();
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (!authentication.IsAuthenticated)
            throw new InvalidOperationException("No se pudo iniciar sesión en Google Play Games.");

        var code = await client.RequestServerSideAccess(WebClientId, false).AsAsync<Java.Lang.String>();
        cancellationToken.ThrowIfCancellationRequested();
        return code?.ToString() is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException("Google no entregó un código de acceso.");
    }
}
