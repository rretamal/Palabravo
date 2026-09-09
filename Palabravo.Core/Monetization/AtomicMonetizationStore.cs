using System.Text.Json;

namespace Palabravo.Core.Monetization;

// Single-process transactions: serialize, flush, then atomically replace the snapshot.
// A process interrupted before the rename leaves the previous committed snapshot intact.
public sealed class AtomicMonetizationStore(string path) : IMonetizationStore
{
    private readonly object sync = new();

    public T Update<T>(Func<MonetizationState, T> update)
    {
        lock (sync)
        {
            var state = File.Exists(path)
                ? JsonSerializer.Deserialize<MonetizationState>(File.ReadAllText(path))
                    ?? throw new InvalidDataException("Estado de monetización inválido.")
                : new MonetizationState();
            // Never reset a corrupt store silently: that would erase a paid entitlement.
            var before = JsonSerializer.Serialize(state);
            var result = update(state);
            var after = JsonSerializer.Serialize(state);
            if (before == after && File.Exists(path)) return result;
            using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, state);
                stream.Flush(flushToDisk: true);
            }
            File.Move(path + ".tmp", path, overwrite: true);
            return result;
        }
    }
}
