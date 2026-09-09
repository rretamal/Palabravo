using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Palabravo.Services;

public interface IAccountDeletionGateway
{
    Task<AccountDeletionGatewayResult> RequestAsync(
        string sessionTicket,
        CancellationToken cancellationToken = default);
}

public sealed record AccountDeletionGatewayResult(bool IsAccepted, string? RequestId = null);

public sealed class AccountDeletionGateway : IAccountDeletionGateway
{
    public const string PublicDeletionPage = "https://palabravo.app/eliminar-datos";
    private static readonly Uri Endpoint = new("https://palabravo.app/api/account/delete");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(60) };

    public async Task<AccountDeletionGatewayResult> RequestAsync(
        string sessionTicket,
        CancellationToken cancellationToken = default)
    {
        string[] analyticsInstanceIds = [];
#if ANDROID || IOS
        analyticsInstanceIds = await Monetization.FirebaseAdapter.CaptureInstanceIdsAsync();
        Monetization.FirebaseAdapter.SetCollection(false);
#endif
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new { analyticsInstanceIds })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionTicket);

        using var response = await _client.SendAsync(request, cancellationToken);
        if ((int)response.StatusCode != 202)
            return new AccountDeletionGatewayResult(false);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<AccountDeletionApiResponse>(
            stream, JsonOptions, cancellationToken);
        return string.IsNullOrWhiteSpace(result?.RequestId)
            ? new AccountDeletionGatewayResult(false)
            : new AccountDeletionGatewayResult(true, result.RequestId);
    }

    private sealed record AccountDeletionApiResponse(string? RequestId, string? Status);
}
