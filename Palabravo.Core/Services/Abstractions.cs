using Palabravo.Core.Models;

namespace Palabravo.Core.Services;

public interface IPuzzleRepository
{
    Task<IReadOnlyList<PuzzleDefinition>> GetAllAsync();
    Task<PuzzleDefinition?> GetByIdAsync(string id);
    Task<PuzzleDefinition> GetDailyAsync(DateOnly date);
}

public interface IProgressStore
{
    Task<PlayerProgress> LoadAsync();
    Task SaveAsync(PlayerProgress progress);
}

public interface IClock
{
    DateOnly Today { get; }
    DateOnly UtcToday { get; }
}

public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    public DateOnly UtcToday => DateOnly.FromDateTime(DateTime.UtcNow);
}
