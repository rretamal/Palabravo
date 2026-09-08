using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Services;

public sealed class GameCoordinator(IPuzzleRepository puzzles, IClock clock)
{
    public PuzzleEngine Engine { get; private set; } = new();
    public GameResult? LastResult { get; private set; }
    public ProgressUpdate? LastProgressUpdate { get; private set; }

    public async Task StartAsync(string puzzleId, PuzzleMode mode)
    {
        var playedOn = mode == PuzzleMode.Daily ? clock.UtcToday : clock.Today;
        var puzzle = mode == PuzzleMode.Daily
            ? await puzzles.GetDailyAsync(playedOn)
            : await puzzles.GetByIdAsync(puzzleId) ?? throw new InvalidOperationException("Reto no encontrado.");

        Engine = new PuzzleEngine();
        Engine.Start(puzzle, mode, playedOn);
        LastResult = null;
        LastProgressUpdate = null;
    }

    public GameResult Finish()
    {
        LastResult = Engine.CreateResult();
        return LastResult;
    }

    public void SetProgressUpdate(ProgressUpdate update) => LastProgressUpdate = update;

    public void Restart()
    {
        var state = Engine.State ?? throw new InvalidOperationException("No hay un reto para repetir.");
        Engine = new PuzzleEngine();
        Engine.Start(state.Puzzle, state.Mode, state.PlayedOn);
        LastResult = null;
        LastProgressUpdate = null;
    }
}
