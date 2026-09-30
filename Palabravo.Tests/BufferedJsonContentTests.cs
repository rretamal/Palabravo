using System.Text.Json;
using Palabravo.Core.Services;

namespace Palabravo.Tests;

public sealed class BufferedJsonContentTests
{
    [Fact]
    public async Task Api_payload_has_a_known_utf8_length_and_web_property_names()
    {
        using var content = BufferedJsonContent.Create(new { CustomId = "abc", Name = "Ágil 🦊" });
        var bytes = await content.ReadAsByteArrayAsync();
        Assert.Equal(bytes.LongLength, content.Headers.ContentLength);
        Assert.Equal("application/json", content.Headers.ContentType!.MediaType);
        using var json = JsonDocument.Parse(bytes);
        Assert.Equal("abc", json.RootElement.GetProperty("customId").GetString());
        Assert.Equal("Ágil 🦊", json.RootElement.GetProperty("name").GetString());
    }
}
