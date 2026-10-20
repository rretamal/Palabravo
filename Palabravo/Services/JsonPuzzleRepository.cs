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
        var cacheFile = Path.Combine(FileSystem.AppDataDirectory, "path_catalog_v3.json");
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
            var json = await client.GetStringAsync(new Uri(new Uri(Monetization.MonetizationSettings.Current.ApiBaseUrl), "../content/v2/puzzles.json"));
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

    private readonly SemaphoreSlim dailyGate = new(1, 1);
    private DailyChallengeCatalog? dailyCatalog;
    private DateTimeOffset dailyCheckedAt;

    public async Task<PuzzleDefinition> GetDailyAsync(DateOnly date)
    {
        await dailyGate.WaitAsync();
        try
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            DailyChallengeCatalog ParseDaily(string json)
            {
                var parsed = JsonSerializer.Deserialize<DailyChallengeCatalog>(json, options)
                    ?? throw new InvalidDataException("Calendario vacío.");
                DailyChallengeValidator.Validate(parsed);
                return parsed;
            }
            var cache = Path.Combine(FileSystem.AppDataDirectory, "daily_calendar_v1.json");
            if (dailyCatalog is null)
            {
                using var reader = new StreamReader(await FileSystem.OpenAppPackageFileAsync("daily.json"));
                dailyCatalog = ParseDaily(await reader.ReadToEndAsync());
                try
                {
                    var saved = ParseDaily(await File.ReadAllTextAsync(cache));
                    if (DailyContentSchedule.CanReplace(dailyCatalog, saved)) dailyCatalog = saved;
                }
                catch { /* Bundled calendar works on the first offline launch. */ }
            }
            if (DateTimeOffset.UtcNow - dailyCheckedAt > TimeSpan.FromMinutes(5))
            {
                dailyCheckedAt = DateTimeOffset.UtcNow;
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
                    var json = await client.GetStringAsync(new Uri(new Uri(Monetization.MonetizationSettings.Current.ApiBaseUrl), "../content/daily.json"));
                    var remote = ParseDaily(json);
                    if (DailyContentSchedule.CanReplace(dailyCatalog, remote))
                    {
                        dailyCatalog = remote;
                        await File.WriteAllTextAsync(cache, json);
                    }
                }
                catch { /* Keep the last validated calendar. */ }
            }
            var selectionFile = Path.Combine(FileSystem.AppDataDirectory, $"daily_selection_{date:yyyy-MM-dd}.json");
            try
            {
                if (DailyContentSchedule.ReadSelection(await File.ReadAllTextAsync(selectionFile), date) is { } saved) return saved;
            }
            catch { /* Select and persist the first available content for this date. */ }
            var selected = DailyContentSchedule.Select(dailyCatalog, date);
            if (selected is null)
            {
                // Dates before activation retain the v6 puzzle and completion identity.
                using var legacy = new StreamReader(await FileSystem.OpenAppPackageFileAsync("legacy-puzzles.json"));
                var old = JsonSerializer.Deserialize<List<PuzzleDefinition>>(await legacy.ReadToEndAsync(), options)!;
                selected = DailyPuzzleSchedule.Select(old, date);
            }
            try { await File.WriteAllTextAsync(selectionFile, DailyContentSchedule.SaveSelection(date, selected)); }
            catch { /* A storage error must not prevent playing the available content. */ }
            return selected;
        }
        finally { dailyGate.Release(); }
    }
}
