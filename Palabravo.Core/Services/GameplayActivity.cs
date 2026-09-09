namespace Palabravo.Core.Services;

// Pauses belong to the application flow, so they also cover an attempt started
// while consent, a purchase or another modal is still pending.
public sealed class GameplayActivity
{
    private PuzzleEngine? engine;
    private IDisposable? enginePause;
    private readonly HashSet<Guid> pauses = [];
    private bool visible;
    private bool foreground;
    public event Action<bool>? Changed;
    public bool IsActive => visible && foreground && pauses.Count == 0 && engine?.State is { IsFinished: false };

    public void Attach(PuzzleEngine value)
    {
        enginePause?.Dispose();
        enginePause = null;
        engine = value;
        Refresh();
    }
    public void SetVisible(bool value) { visible = value; Refresh(); }
    public void SetForeground(bool value) { foreground = value; Refresh(); }
    public void Touch() => Refresh();
    public IDisposable Pause()
    {
        var id = Guid.NewGuid();
        pauses.Add(id);
        Refresh();
        return new Scope(() => { pauses.Remove(id); Refresh(); });
    }
    private void Refresh()
    {
        if (IsActive) { enginePause?.Dispose(); enginePause = null; }
        else if (engine is not null) enginePause ??= engine.Pause();
        Changed?.Invoke(IsActive);
    }
    private sealed class Scope(Action release) : IDisposable
    {
        private bool disposed;
        public void Dispose() { if (disposed) return; disposed = true; release(); }
    }
}
