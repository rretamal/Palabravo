using Palabravo.Core.Services;

namespace Palabravo.Tests;

public sealed class ReferralLinkTests
{
    [Theory]
    [InlineData("https://palabravo.app/reto/puzzle-07", "puzzle-07")]
    [InlineData("https://palabravo.app/reto/07", "puzzle-07")]
    [InlineData("https://evil.example/reto/puzzle-07", null)]
    [InlineData("http://palabravo.app/reto/puzzle-07", null)]
    [InlineData("https://palabravo.app/reto/puzzle-31", "puzzle-31")]
    [InlineData("https://palabravo.app/reto/puzzle-07/extra", null)]
    public void Accepts_only_known_challenges_on_the_canonical_https_host(string url, string? expected) =>
        Assert.Equal(expected, ReferralLink.Parse(new Uri(url)));
}
