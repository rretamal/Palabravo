using System.Text;
using System.Text.Json;

namespace Palabravo.Core.Services;

public static class BufferedJsonContent
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    // The API gateway rejects chunked request bodies. Buffer small JSON payloads
    // so the Android HTTP handler can send their Content-Length.
    public static StringContent Create<T>(T value, JsonSerializerOptions? options = null) =>
        new(JsonSerializer.Serialize(value, options ?? WebOptions), Encoding.UTF8, "application/json");
}
