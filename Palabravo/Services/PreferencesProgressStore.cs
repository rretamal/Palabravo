using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Services;

public sealed class PreferencesProgressStore : IProgressStore
{
    private const string Key = "player_progress_v1";
    public Task<PlayerProgress> LoadAsync()
    {
        var json = Preferences.Default.Get(Key, string.Empty);
        var progress = ProgressJson.DeserializeSafe(json, out var migrated);
        if (migrated)
            Preferences.Default.Set(Key, ProgressJson.Serialize(progress));
        return Task.FromResult(progress);
    }

    public Task SaveAsync(PlayerProgress progress)
    {
        Preferences.Default.Set(Key, ProgressJson.Serialize(progress));
        return Task.CompletedTask;
    }
}
