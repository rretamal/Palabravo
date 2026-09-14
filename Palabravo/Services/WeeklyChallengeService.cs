using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Palabravo.Core.Models;
using Palabravo.Core.Services;
using Palabravo.Services.Monetization;

namespace Palabravo.Services;

public sealed record WeeklyRankingItem(int Rank, string Name, long Score, bool IsCurrentPlayer);
public sealed record WeeklyRankingSnapshot(bool IsAvailable, string? Error,
    IReadOnlyList<WeeklyRankingItem> Entries, WeeklyRankingItem? CurrentPlayer, int? Percentile);
public sealed record WeeklyInvite(string Token, string WeeklyId, string Challenger,
    long Score, int TimeSeconds, int Mistakes, string Medal);

public sealed class WeeklyChallengeService(PlayFabLeaderboardService accounts)
{
    private const string CacheKey = "weekly_content_cache_v1";
    private const string PendingResultKey = "weekly_pending_result_v1";
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(8) };
    private readonly SemaphoreSlim submissionLock = new(1, 1);
    private IReadOnlyList<WeeklyChallengeDefinition> catalog = [];
    private WeeklyChallengeDefinition? current;
    private bool loaded;
    private DateTimeOffset lastRemoteRefresh;

    public async Task<WeeklyChallengeDefinition?> GetCurrentAsync(bool refresh = false)
    {
        if (loaded && !refresh)
        {
            current = SelectCurrent(catalog);
            if (DateTimeOffset.UtcNow - lastRemoteRefresh > TimeSpan.FromMinutes(5))
                _ = RefreshRemoteAsync();
            return current;
        }

        if (!loaded)
        {
            loaded = true;
            catalog = ParseCatalog(Preferences.Default.Get(CacheKey, string.Empty));
            current = SelectCurrent(catalog);
            if (catalog.Count == 0)
            {
                try
                {
                    await using var stream = await FileSystem.OpenAppPackageFileAsync("weekly.json");
                    using var reader = new StreamReader(stream);
                    catalog = ParseCatalog(await reader.ReadToEndAsync());
                    current = SelectCurrent(catalog);
                }
                catch { catalog = []; current = null; }
            }
            if (current?.IsActive(DateTimeOffset.UtcNow) == true)
            {
                _ = RefreshRemoteAsync();
                return current;
            }
        }

        await RefreshRemoteAsync();
        return current?.IsActive(DateTimeOffset.UtcNow) == true ? current : null;
    }

    public async Task<WeeklyChallengeDefinition?> GetByIdAsync(string id)
    {
        await GetCurrentAsync();
        return catalog.FirstOrDefault(weekly => weekly.IsActive(DateTimeOffset.UtcNow)
            && string.Equals(weekly.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> SubmitResultAsync(WeeklyChallengeDefinition weekly, GameResult result)
    {
        SavePendingResult(CreatePendingResult(weekly.Id, result));
        return await FlushPendingResultAsync(weekly.Id);
    }

    public void QueueStoredResult(WeeklyChallengeDefinition weekly, CompletionRecord completion)
    {
        var result = new GameResult(completion.PuzzleId, weekly.Puzzle.Title, PuzzleMode.Weekly,
            completion.CompletedOn, true, completion.BestMedal, completion.Errors, completion.HintsUsed,
            false, TimeSpan.FromSeconds(completion.ElapsedSeconds), [], []);
        SavePendingResult(CreatePendingResult(weekly.Id, result));
    }

    public async Task<WeeklyRankingSnapshot> GetRankingAsync(string weeklyId)
    {
        try
        {
            await FlushPendingResultAsync(weeklyId);
            using var request = await AuthorizedAsync(HttpMethod.Get,
                $"weekly/{Uri.EscapeDataString(weeklyId)}/ranking");
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<WeeklyRankingSnapshot>(JsonOptions)
                ?? new(false, "Respuesta vacía.", [], null, null);
        }
        catch
        {
            return new(false, "No pudimos cargar el ranking semanal. Tu resultado queda guardado para reintentarlo.",
                [], null, null);
        }
    }

    public async Task<WeeklyInvite?> CreateInviteAsync(WeeklyChallengeDefinition weekly, GameResult result)
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Post, "challenges",
                new { weeklyId = weekly.Id, score = LeaderboardScore.Encode(result), timeSeconds = ScoreSeconds(result),
                    mistakes = result.Errors, hints = result.HintsUsed, medal = result.Medal.ToString().ToLowerInvariant(),
                    challenger = accounts.GetSnapshot().DisplayName });
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<WeeklyInvite>(JsonOptions);
        }
        catch { return null; }
    }

    public async Task<WeeklyInvite?> ResolveInviteAsync(string token)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(token, "^[A-Z0-9]{6}$")) return null;
        try
        {
            return await client.GetFromJsonAsync<WeeklyInvite>(
                new Uri(new Uri(MonetizationSettings.Current.ApiBaseUrl), $"challenges/{token}"), JsonOptions);
        }
        catch { return null; }
    }

    public static string ChallengeUrl(string token) => $"https://palabravo.app/challenge/{token}";

    private static int ScoreSeconds(GameResult result) =>
        Math.Clamp((int)Math.Round(result.Elapsed.TotalSeconds), 0, 99_999);

    private async Task<HttpRequestMessage> AuthorizedAsync(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, new Uri(new Uri(MonetizationSettings.Current.ApiBaseUrl), path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await accounts.GetSessionTicketAsync());
        if (body is not null) request.Content = JsonContent.Create(body, options: JsonOptions);
        return request;
    }

    private void SavePendingResult(PendingWeeklyResult pending)
    {
        var saved = ParsePendingResult(Preferences.Default.Get(PendingResultKey, string.Empty));
        if (saved is not null && string.Equals(saved.WeeklyId, pending.WeeklyId, StringComparison.OrdinalIgnoreCase)
            && saved.Score >= pending.Score)
            return;

        Preferences.Default.Set(PendingResultKey, JsonSerializer.Serialize(pending, JsonOptions));
    }

    private PendingWeeklyResult CreatePendingResult(string weeklyId, GameResult result) =>
        new(weeklyId, LeaderboardScore.Encode(result), ScoreSeconds(result),
            Math.Clamp(result.Errors, 0, 4), Math.Clamp(result.HintsUsed, 0, 2),
            result.Medal.ToString().ToLowerInvariant(), accounts.GetSnapshot().DisplayName);

    private async Task<bool> FlushPendingResultAsync(string weeklyId)
    {
        await submissionLock.WaitAsync();
        try
        {
            var pending = ParsePendingResult(Preferences.Default.Get(PendingResultKey, string.Empty));
            if (pending is null || !string.Equals(pending.WeeklyId, weeklyId, StringComparison.OrdinalIgnoreCase))
                return pending is null;

            using var request = await AuthorizedAsync(HttpMethod.Post, "weekly/results", pending);
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return false;

            Preferences.Default.Remove(PendingResultKey);
            return true;
        }
        catch { return false; }
        finally { submissionLock.Release(); }
    }

    private static PendingWeeklyResult? ParsePendingResult(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<PendingWeeklyResult>(json, JsonOptions); }
        catch { return null; }
    }

    internal static IReadOnlyList<WeeklyChallengeDefinition> ParseCatalog(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var parsed = JsonSerializer.Deserialize<WeeklyChallengeCatalog>(json, JsonOptions);
            if (parsed is null || parsed.Version < 1 || parsed.Challenges.Count == 0)
                return [];
            foreach (var weekly in parsed.Challenges)
                WeeklyChallengeValidator.Validate(weekly);
            if (parsed.Challenges.Select(item => item.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                != parsed.Challenges.Count)
                return [];
            return parsed.Challenges;
        }
        catch { return []; }
    }

    private static WeeklyChallengeDefinition? SelectCurrent(IEnumerable<WeeklyChallengeDefinition> challenges) =>
        challenges.Where(item => item.IsActive(DateTimeOffset.UtcNow))
            .OrderByDescending(item => item.StartsAt).FirstOrDefault();

    private async Task RefreshRemoteAsync()
    {
        lastRemoteRefresh = DateTimeOffset.UtcNow;
        try
        {
            var contentUri = new Uri(new Uri(MonetizationSettings.Current.ApiBaseUrl), "../content/weekly.json");
            var json = await client.GetStringAsync(contentUri);
            var downloaded = ParseCatalog(json);
            if (downloaded.Count == 0) return;
            catalog = downloaded;
            current = SelectCurrent(catalog);
            Preferences.Default.Set(CacheKey, json);
        }
        catch { /* The last valid or bundled event remains available. */ }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private sealed record PendingWeeklyResult(string WeeklyId, long Score, int TimeSeconds,
        int Mistakes, int Hints, string Medal, string Name);
}
