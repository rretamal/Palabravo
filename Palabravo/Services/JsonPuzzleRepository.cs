using System.Text.Json;
using System.Text.Json.Serialization;
using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Services;

public sealed class JsonPuzzleRepository : IPuzzleRepository
{
    private IReadOnlyList<PuzzleDefinition>? _cache;
    private DateTimeOffset checkedAt;
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<IReadOnlyList<PuzzleDefinition>> GetAllAsync()
    {
        if (_cache is not null && DateTimeOffset.UtcNow - checkedAt < TimeSpan.FromMinutes(5))
            return _cache;
        await gate.WaitAsync();
        try
        {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        List<PuzzleDefinition> Parse(string json)
        {
            var data = JsonSerializer.Deserialize<List<PuzzleDefinition>>(json, options) ?? [];
            PuzzleCatalogValidator.Validate(data); return data;
        }
        var cacheFile = Path.Combine(FileSystem.AppDataDirectory, "path_catalog.json");
        if (_cache is null)
        {
            try { _cache = Parse(await File.ReadAllTextAsync(cacheFile)); } catch { }
            if (_cache is null)
            {
                await using var stream = await FileSystem.OpenAppPackageFileAsync("puzzles.json");
                using var reader = new StreamReader(stream);
                _cache = Parse(await reader.ReadToEndAsync());
            }
        }
        checkedAt = DateTimeOffset.UtcNow;
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
            var json = await client.GetStringAsync(new Uri(new Uri(Monetization.MonetizationSettings.Current.ApiBaseUrl), "../content/puzzles.json"));
            var remote = Parse(json);
            if (_cache.All(old => remote.Any(p => p.Id == old.Id && p.Order == old.Order && p.ContentVersion >= old.ContentVersion)))
            {
                _cache = remote.OrderBy(p => p.Order).ToList();
                await File.WriteAllTextAsync(cacheFile, json);
            }
        }
        catch { /* Keep the last validated or bundled catalogue. */ }
        return _cache;
        }
        finally { gate.Release(); }
    }

    public async Task<PuzzleDefinition?> GetByIdAsync(string id) =>
        (await GetAllAsync()).FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));

    public async Task<PuzzleDefinition> GetDailyAsync(DateOnly date)
    {
        var puzzles = await GetAllAsync();
        return DailyPuzzleSchedule.Select(puzzles, date);
    }
}
