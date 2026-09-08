using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Services;

public interface ILeaderboardService
{
    Task<bool> SubmitDailyResultAsync(GameResult result);
    Task<LeaderboardSnapshot> GetDailyAsync();
}

public sealed record LeaderboardEntry(
    int Rank,
    string Name,
    long Score,
    bool IsCurrentPlayer);

public sealed record LeaderboardSnapshot(
    bool IsAvailable,
    string? Error,
    IReadOnlyList<LeaderboardEntry> Entries,
    LeaderboardEntry? CurrentPlayer,
    DateTimeOffset? NextReset);

public sealed class PlayFabLeaderboardService(
    IGooglePlayGamesAuthService googlePlayGames,
    IAccountDeletionGateway accountDeletionGateway)
    : ILeaderboardService, IPlayerAccountService
{
    public const string TitleId = "153ECF";
    private const string StatisticName = "DailyScore";
    private const string LeaderboardName = "DailyRanking";
    private const string GuestIdKey = "playfab_guest_id_v1";
    private const string GuestAliasKey = "playfab_guest_alias_v1";
    private const string AliasConfiguredKey = "playfab_alias_configured_v1";
    private const string GoogleLinkedKey = "playfab_google_linked_v1";
    private const string PendingScoreKey = "playfab_pending_daily_score_v2";
    private const string PendingDateKey = "playfab_pending_daily_date_v2";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client = new()
    {
        BaseAddress = new Uri($"https://{TitleId}.playfabapi.com/"),
        Timeout = TimeSpan.FromSeconds(8)
    };
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private PlayFabSession? _session;

    public PlayerAccountSnapshot GetSnapshot() => new(
        GetOrCreateAlias(),
        Preferences.Default.Get(GoogleLinkedKey, false),
        googlePlayGames.IsSupported);

    public async Task<PlayerAccountActionResult> ContinueWithGoogleAsync(
        CancellationToken cancellationToken = default)
    {
        if (!googlePlayGames.IsSupported)
            return new PlayerAccountActionResult(false, "Google Play Games solo está disponible en Android.");

        try
        {
            var guestSession = await GetSessionAsync();
            var authCode = await googlePlayGames.RequestServerAuthCodeAsync(cancellationToken);
            try
            {
                await PostAsync<EmptyData>(
                    "Client/LinkGooglePlayGamesServicesAccount",
                    new { ServerAuthCode = authCode, ForceLink = false },
                    "X-Authorization",
                    guestSession.SessionTicket);
            }
            catch (PlayFabException exception) when (exception.Code is
                "AccountAlreadyLinked" or "LinkedAccountAlreadyClaimed")
            {
                // El codigo es de un solo uso: se solicita otro antes de iniciar sesion.
                authCode = await googlePlayGames.RequestServerAuthCodeAsync(cancellationToken);
                var login = await PostAsync<LoginData>(
                    "Client/LoginWithGooglePlayGamesServices",
                    new { TitleId, ServerAuthCode = authCode, CreateAccount = false });
                var googleSession = await CreateSessionAsync(login);
                await PostAsync<EmptyData>(
                    "Client/LinkCustomID",
                    new { CustomId = GetOrCreateGuestId(), ForceLink = true },
                    "X-Authorization",
                    googleSession.SessionTicket);
                _session = googleSession;
            }

            Preferences.Default.Set(GoogleLinkedKey, true);
            return new PlayerAccountActionResult(true, "Tu cuenta quedó vinculada con Google Play Games.");
        }
        catch (OperationCanceledException)
        {
            return new PlayerAccountActionResult(false, "Inicio de sesión cancelado.");
        }
        catch (PlayFabException exception) when (exception.Code is
            "GoogleOAuthNotConfiguredForTitle" or "InvalidGooglePlayGamesServerAuthCode")
        {
            return new PlayerAccountActionResult(false, "Google Play Games todavía no está configurado correctamente.");
        }
        catch
        {
            return new PlayerAccountActionResult(false, "No pudimos conectar con Google Play Games. Inténtalo nuevamente.");
        }
    }

    public async Task<PlayerAccountDeletionResult> DeleteAccountAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await GetSessionAsync();
            var deletion = await accountDeletionGateway.RequestAsync(
                session.SessionTicket, cancellationToken);
            if (!deletion.IsAccepted)
                return new PlayerAccountDeletionResult(false,
                    "No pudimos aceptar la eliminación. Inténtalo nuevamente.");

            ClearLocalAccountData();
            _session = null;
            return new PlayerAccountDeletionResult(true,
                "La solicitud fue aceptada. Tu cuenta y sus datos están siendo eliminados.",
                deletion.RequestId);
        }
        catch (OperationCanceledException)
        {
            return new PlayerAccountDeletionResult(false, "La solicitud fue cancelada.");
        }
        catch
        {
            return new PlayerAccountDeletionResult(false,
                "No pudimos conectar con el servicio de eliminación. Tus datos siguen intactos.");
        }
    }

    public async Task<bool> SubmitDailyResultAsync(GameResult result)
    {
        if (!result.IsSuccess || result.Mode != PuzzleMode.Daily)
            return false;

        var score = LeaderboardScore.Encode(result);
        try
        {
            var session = await GetSessionAsync();
            await UpdateScoreAsync(session, score);
            ClearPendingScore();
            return true;
        }
        catch
        {
            Preferences.Default.Set(PendingScoreKey, score.ToString(CultureInfo.InvariantCulture));
            Preferences.Default.Set(PendingDateKey, result.PlayedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            return false;
        }
    }

    public async Task<LeaderboardSnapshot> GetDailyAsync()
    {
        try
        {
            var session = await GetSessionAsync();
            await FlushPendingScoreAsync(session);
            var response = await PostAsync<LeaderboardData>(
                "Leaderboard/GetLeaderboard",
                new { LeaderboardName, StartingPosition = 1, PageSize = 50 },
                "X-EntityToken",
                session.EntityToken);

            var entries = MapEntries(response.Rankings, session.Entity.Id);
            var current = entries.FirstOrDefault(entry => entry.IsCurrentPlayer);
            if (current is null)
            {
                var around = await PostAsync<LeaderboardData>(
                    "Leaderboard/GetLeaderboardAroundEntity",
                    new
                    {
                        LeaderboardName,
                        MaxSurroundingEntries = 2,
                        Entity = session.Entity
                    },
                    "X-EntityToken",
                    session.EntityToken);
                current = MapEntries(around.Rankings, session.Entity.Id)
                    .FirstOrDefault(entry => entry.IsCurrentPlayer);
            }

            return new LeaderboardSnapshot(true, null, entries, current, response.NextReset);
        }
        catch (Exception exception)
        {
            return new LeaderboardSnapshot(false, FriendlyError(exception), [], null, null);
        }
    }

    private async Task<PlayFabSession> GetSessionAsync()
    {
        if (_session is { } cached && cached.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
            return cached;

        await _sessionLock.WaitAsync();
        try
        {
            if (_session is { } current && current.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
                return current;

            var guestId = GetOrCreateGuestId();
            var login = await PostAsync<LoginData>(
                "Client/LoginWithCustomID",
                new { TitleId, CustomId = guestId, CreateAccount = true });
            _session = await CreateSessionAsync(login);
            await EnsureAliasAsync(_session);
            return _session;
        }
        finally
        {
            _sessionLock.Release();
        }
    }

    private async Task<PlayFabSession> CreateSessionAsync(LoginData login)
    {
        var entityToken = login.EntityToken;
        if (entityToken?.EntityToken is null || entityToken.Entity is null)
        {
            entityToken = await PostAsync<EntityTokenData>(
                "Authentication/GetEntityToken",
                new { },
                "X-Authorization",
                login.SessionTicket);
        }

        return new PlayFabSession(
            login.SessionTicket,
            entityToken.EntityToken!,
            entityToken.Entity!,
            entityToken.TokenExpiration ?? DateTimeOffset.UtcNow.AddHours(1));
    }

    private async Task EnsureAliasAsync(PlayFabSession session)
    {
        if (Preferences.Default.Get(AliasConfiguredKey, false))
            return;

        try
        {
            await PostAsync<SetDisplayNameData>(
                "Profile/SetDisplayName",
                new { DisplayName = GetOrCreateAlias(), Entity = session.Entity },
                "X-EntityToken",
                session.EntityToken);
            Preferences.Default.Set(AliasConfiguredKey, true);
        }
        catch
        {
            // El ranking sigue siendo utilizable con el alias derivado del identificador.
        }
    }

    private Task UpdateScoreAsync(PlayFabSession session, long score) =>
        PostAsync<UpdateStatisticsData>(
            "Statistic/UpdateStatistics",
            new
            {
                Entity = session.Entity,
                Statistics = new[]
                {
                    new
                    {
                        Name = StatisticName,
                        Scores = new[] { score.ToString(CultureInfo.InvariantCulture) }
                    }
                }
            },
            "X-EntityToken",
            session.EntityToken);

    private async Task FlushPendingScoreAsync(PlayFabSession session)
    {
        var date = Preferences.Default.Get(PendingDateKey, string.Empty);
        var scoreText = Preferences.Default.Get(PendingScoreKey, string.Empty);
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var pendingDate)
            || !long.TryParse(scoreText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var score))
            return;

        if (pendingDate != DateOnly.FromDateTime(DateTime.UtcNow))
        {
            ClearPendingScore();
            return;
        }

        await UpdateScoreAsync(session, score);
        ClearPendingScore();
    }

    private static IReadOnlyList<LeaderboardEntry> MapEntries(
        IReadOnlyList<LeaderboardEntryData>? rankings,
        string currentEntityId) =>
        rankings?.Select(entry =>
        {
            var entityId = entry.Entity?.Id ?? string.Empty;
            var name = string.IsNullOrWhiteSpace(entry.DisplayName)
                ? FallbackName(entityId)
                : entry.DisplayName;
            _ = long.TryParse(entry.Scores?.FirstOrDefault(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var score);
            return new LeaderboardEntry(entry.Rank, name, score,
                string.Equals(entityId, currentEntityId, StringComparison.Ordinal));
        }).ToList() ?? [];

    private async Task<T> PostAsync<T>(
        string path,
        object body,
        string? authorizationHeader = null,
        string? token = null)
        where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        if (authorizationHeader is not null && token is not null)
            request.Headers.TryAddWithoutValidation(authorizationHeader, token);

        using var response = await _client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        PlayFabEnvelope<T>? envelope = null;
        try
        {
            envelope = JsonSerializer.Deserialize<PlayFabEnvelope<T>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            // El error uniforme de abajo evita filtrar respuestas remotas a la interfaz.
        }

        if (!response.IsSuccessStatusCode || envelope?.Data is null)
            throw new PlayFabException(
                envelope?.Error,
                envelope?.ErrorMessage ?? "PlayFab no respondió correctamente.");

        return envelope.Data;
    }

    private static string GetOrCreateGuestId()
    {
        var existing = Preferences.Default.Get(GuestIdKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        var created = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(GuestIdKey, created);
        return created;
    }

    private static string GetOrCreateAlias()
    {
        var existing = Preferences.Default.Get(GuestAliasKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        string[] adjectives = ["Ágil", "Curioso", "Audaz", "Sereno", "Brillante", "Ingenioso"];
        string[] nouns = ["Lince", "Zorro", "Búho", "Puma", "Cóndor", "Delfín"];
        var guestId = GetOrCreateGuestId();
        var seed = Convert.ToInt32(guestId[..6], 16);
        var alias = $"{adjectives[seed % adjectives.Length]} {nouns[(seed / adjectives.Length) % nouns.Length]} {guestId[^3..].ToUpperInvariant()}";
        Preferences.Default.Set(GuestAliasKey, alias);
        return alias;
    }

    private static string FallbackName(string entityId) =>
        string.IsNullOrWhiteSpace(entityId)
            ? "Jugador invitado"
            : $"Jugador {entityId[^Math.Min(4, entityId.Length)..].ToUpperInvariant()}";

    private static string FriendlyError(Exception exception) => exception switch
    {
        TaskCanceledException => "La conexión tardó demasiado. Inténtalo nuevamente.",
        HttpRequestException => "No hay conexión con el ranking.",
        _ => "No pudimos cargar el ranking en este momento."
    };

    private static void ClearPendingScore()
    {
        Preferences.Default.Remove(PendingScoreKey);
        Preferences.Default.Remove(PendingDateKey);
    }

    private static void ClearLocalAccountData()
    {
        Preferences.Default.Remove(GuestIdKey);
        Preferences.Default.Remove(GuestAliasKey);
        Preferences.Default.Remove(AliasConfiguredKey);
        Preferences.Default.Remove(GoogleLinkedKey);
        ClearPendingScore();
    }

    private sealed record PlayFabSession(
        string SessionTicket,
        string EntityToken,
        EntityKey Entity,
        DateTimeOffset ExpiresAt);
    private sealed record PlayFabEnvelope<T>(T? Data, string? Error, string? ErrorMessage);
    private sealed class PlayFabException(string? code, string message) : InvalidOperationException(message)
    {
        public string? Code { get; } = code;
    }
    private sealed record LoginData(string SessionTicket, EntityTokenData? EntityToken);
    private sealed record EntityTokenData(string? EntityToken, EntityKey? Entity, DateTimeOffset? TokenExpiration);
    private sealed record EntityKey(string Id, string Type);
    private sealed record SetDisplayNameData;
    private sealed record EmptyData;
    private sealed record UpdateStatisticsData;
    private sealed record LeaderboardData(
        IReadOnlyList<LeaderboardEntryData>? Rankings,
        DateTimeOffset? NextReset);
    private sealed record LeaderboardEntryData(
        int Rank,
        string? DisplayName,
        EntityKey? Entity,
        IReadOnlyList<string>? Scores);
}
