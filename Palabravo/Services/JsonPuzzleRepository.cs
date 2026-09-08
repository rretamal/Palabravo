using System.Text.Json;
using System.Text.Json.Serialization;
using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Services;

public sealed class JsonPuzzleRepository : IPuzzleRepository
{
    private IReadOnlyList<PuzzleDefinition>? _cache;

    public async Task<IReadOnlyList<PuzzleDefinition>> GetAllAsync()
    {
        if (_cache is not null)
            return _cache;

        await using var stream = await FileSystem.OpenAppPackageFileAsync("puzzles.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        var puzzles = await JsonSerializer.DeserializeAsync<List<PuzzleDefinition>>(stream, options) ?? [];
        PuzzleCatalogValidator.Validate(puzzles);
        _cache = puzzles.OrderBy(x => x.Order).ToList();
        return _cache;
    }

    public async Task<PuzzleDefinition?> GetByIdAsync(string id) =>
        (await GetAllAsync()).FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));

    public async Task<PuzzleDefinition> GetDailyAsync(DateOnly date)
    {
        var puzzles = await GetAllAsync();
        return DailyPuzzleSchedule.Select(puzzles, date);
    }
}
