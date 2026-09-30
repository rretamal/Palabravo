using Palabravo.Core.Services;

namespace Palabravo.Tests;

public sealed class PlayerApiAuthorizationTests
{
    [Fact]
    public void Player_ticket_survives_a_gateway_authorization_replacement()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://palabravo.app/api/path/ranking");
        PlayerApiAuthorization.Set(request, "player-session");
        request.Headers.Authorization = new("Bearer", "gateway-token");
        Assert.Equal("Bearer player-session", Assert.Single(request.Headers.GetValues(PlayerApiAuthorization.HeaderName)));
    }

    [Fact]
    public void A_new_session_replaces_the_existing_ticket_in_both_headers()
    {
        using var request = new HttpRequestMessage();
        PlayerApiAuthorization.Set(request, "old-session");
        PlayerApiAuthorization.Set(request, "new-session");
        Assert.Equal("Bearer new-session", Assert.Single(request.Headers.GetValues(PlayerApiAuthorization.HeaderName)));
        Assert.Equal("Bearer new-session", request.Headers.Authorization!.ToString());
    }
}
