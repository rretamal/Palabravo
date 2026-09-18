using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Palabravo.Core.Models;
using Palabravo.Core.Services;
using Palabravo.Services.Monetization;

namespace Palabravo.Services;

public sealed record PathRankingItem(int Rank, string Name, int Score, int Completed,
    int Gold, int Silver, int Bronze, bool IsCurrentPlayer);
public sealed record PathRankingSnapshot(bool IsAvailable, string? Error,
    IReadOnlyList<PathRankingItem> Entries, PathRankingItem? CurrentPlayer,
    int? Percentile, int? PointsToNext, bool PendingSync);

public sealed class PathRankingService(PlayFabLeaderboardService accounts)
{
    private const string PendingResultKey = "path_ranking_pending_result_v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(8) };
    private readonly SemaphoreSlim submissionLock = new(1, 1);

    public async Task<bool> SubmitAsync(PlayerProgress progress)
    {
        Queue(progress);
        return await FlushAsync();
    }

    public async Task<PathRankingSnapshot> GetRankingAsync(PlayerProgress progress)
    {
        if (PathRankingScore.Calculate(progress).Completed > 0) Queue(progress);
        var synchronized = await FlushAsync();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                new Uri(new Uri(MonetizationSettings.Current.ApiBaseUrl), "path/ranking"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await accounts.GetSessionTicketAsync());
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var remote = await response.Content.ReadFromJsonAsync<RemoteSnapshot>(JsonOptions);
            return remote is null
                ? new(false, "El ranking respondió sin datos.", [], null, null, null, !synchronized)
                : new(remote.IsAvailable, remote.Error, remote.Entries, remote.CurrentPlayer,
                    remote.Percentile, remote.PointsToNext, !synchronized);
        }
        catch
        {
            return new(false, "No pudimos cargar el ranking del camino. Tu puntaje queda guardado para reintentarlo.",
                [], null, null, null, !synchronized);
        }
    }

    private void Queue(PlayerProgress progress)
    {
        var score = PathRankingScore.Calculate(progress);
        if (score.Completed == 0) return;
        var pending = new PendingPathResult(accounts.GetSnapshot().DisplayName, score.Score,
            score.Completed, score.Gold, score.Silver, score.Bronze);
        Preferences.Default.Set(PendingResultKey, JsonSerializer.Serialize(pending, JsonOptions));
    }

    private async Task<bool> FlushAsync()
    {
        await submissionLock.WaitAsync();
        try
        {
            var json = Preferences.Default.Get(PendingResultKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return true;
            PendingPathResult? pending;
            try { pending = JsonSerializer.Deserialize<PendingPathResult>(json, JsonOptions); }
            catch { pending = null; }
            if (pending is null) { Preferences.Default.Remove(PendingResultKey); return true; }

            using var request = new HttpRequestMessage(HttpMethod.Post,
                new Uri(new Uri(MonetizationSettings.Current.ApiBaseUrl), "path/results"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await accounts.GetSessionTicketAsync());
            request.Content = JsonContent.Create(pending, options: JsonOptions);
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return false;
            // A newer completion may have been queued while this request was in flight.
            if (string.Equals(Preferences.Default.Get(PendingResultKey, string.Empty), json,
                StringComparison.Ordinal))
                Preferences.Default.Remove(PendingResultKey);
            return true;
        }
        catch { return false; }
        finally { submissionLock.Release(); }
    }

    private sealed record PendingPathResult(string Name, int Score, int Completed,
        int Gold, int Silver, int Bronze);
    private sealed record RemoteSnapshot(bool IsAvailable, string? Error,
        IReadOnlyList<PathRankingItem> Entries, PathRankingItem? CurrentPlayer,
        int? Percentile, int? PointsToNext);
}
