using Android.Gms.Extensions;
using Android.Runtime;
using Android.Gms.Games;

namespace Palabravo.Services;

public sealed class GooglePlayGamesAuthService : IGooglePlayGamesAuthService
{
    public bool IsSupported => true;

    public async Task<string?> GetPlayerNameAsync()
    {
        var activity = Platform.CurrentActivity;
        if (activity is null) return null;
        // PlayersClient is present in the SDK but omitted by this .NET binding.
        using var games = Java.Lang.Class.ForName("com.google.android.gms.games.PlayGames");
        using var activityClass = Java.Lang.Class.ForName("android.app.Activity");
        using var factory = games.GetMethod("getPlayersClient", activityClass);
        using var client = factory.Invoke(null, activity);
        using var players = Java.Lang.Class.ForName("com.google.android.gms.games.PlayersClient");
        using var method = players.GetMethod("getCurrentPlayer");
        using var taskObject = method.Invoke(client);
        using var task = taskObject!.JavaCast<Android.Gms.Tasks.Task>();
        using var player = await task.AsAsync<Java.Lang.Object>();
        using var playerClass = Java.Lang.Class.ForName("com.google.android.gms.games.Player");
        using var displayName = playerClass.GetMethod("getDisplayName");
        using var name = displayName.Invoke(player);
        return name?.ToString();
    }

    public async Task<string> RequestServerAuthCodeAsync(CancellationToken cancellationToken = default)
    {
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("No hay una actividad Android disponible.");
        try
        {
            var client = PlayGames.GetGamesSignInClient(activity);
            var webClientId = activity.GetString(Resource.String.google_play_games_web_client_id);

            var authentication = await client.IsAuthenticated().AsAsync<AuthenticationResult>();
            cancellationToken.ThrowIfCancellationRequested();
            if (!authentication.IsAuthenticated)
            {
                authentication = await client.SignIn().AsAsync<AuthenticationResult>();
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (!authentication.IsAuthenticated)
                throw new InvalidOperationException("No se pudo iniciar sesión en Google Play Games.");

            var code = await client.RequestServerSideAccess(webClientId, false).AsAsync<Java.Lang.String>();
            cancellationToken.ThrowIfCancellationRequested();
            return code?.ToString() is { Length: > 0 } value
                ? value
                : throw new InvalidOperationException("Google no entregó un código de acceso.");
        }
        catch (Android.Gms.Common.Apis.ApiException exception)
        {
            throw new GooglePlayGamesSignInException(exception.StatusCode, exception);
        }
    }
}
