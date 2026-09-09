using System.Net.Http.Headers;
using System.Net.Http.Json;
using Palabravo.Core.Monetization;

namespace Palabravo.Services.Monetization;

public sealed class EntitlementGateway(PlayFabLeaderboardService account) : IEntitlementGateway
{
    private readonly HttpClient client = new() { BaseAddress = new(MonetizationSettings.Current.ApiBaseUrl), Timeout = TimeSpan.FromSeconds(15) };
    public Task<Entitlement> VerifyAsync(PurchaseProof proof) => SendAsync(HttpMethod.Post, "purchases/verify", proof);
    public Task<Entitlement> GetAsync() => SendAsync(HttpMethod.Get, $"purchases/entitlement?platform={PlatformName}", null);
    private static string PlatformName => DeviceInfo.Platform == DevicePlatform.iOS ? "ios" : "android";
    private async Task<Entitlement> SendAsync(HttpMethod method, string path, PurchaseProof? proof)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await account.GetSessionTicketAsync());
        if (proof is not null) request.Content = JsonContent.Create(proof);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Entitlement>() ?? new(false, false, "unavailable");
    }
}
