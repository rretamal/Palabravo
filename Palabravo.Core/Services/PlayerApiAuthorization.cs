using System.Net.Http.Headers;

namespace Palabravo.Core.Services;

public static class PlayerApiAuthorization
{
    public const string HeaderName = "X-Palabravo-Authorization";

    public static void Set(HttpRequestMessage request, string sessionTicket)
    {
        var authorization = new AuthenticationHeaderValue("Bearer", sessionTicket);
        request.Headers.Authorization = authorization;
        // Azure Static Web Apps owns Authorization; preserve the player ticket
        // in an application header when the gateway forwards the request.
        request.Headers.Remove(HeaderName);
        request.Headers.Add(HeaderName, authorization.ToString());
    }
}
