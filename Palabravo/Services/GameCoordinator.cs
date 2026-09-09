using Palabravo.Core.Models;
using Palabravo.Core.Services;
using Palabravo.Core.Monetization;

namespace Palabravo.Services;

public sealed class GameCoordinator(IPuzzleRepository puzzles, IClock clock, IMonetizationService monetization,
    GameplayActivity activity, WeeklyChallengeService weeklyChallenges)
{
    public PuzzleEngine Engine { get; private set; } = new();
    public GameResult? LastResult { get; private set; }
    public ProgressUpdate? LastProgressUpdate { get; private set; }
    public WeeklyChallengeDefinition? CurrentWeekly { get; private set; }

    public async Task StartAsync(string puzzleId, PuzzleMode mode, bool referred = false)
    {
        var playedOn = mode == PuzzleMode.Daily ? clock.UtcToday : clock.Today;
        CurrentWeekly = mode == PuzzleMode.Weekly
            ? await weeklyChallenges.GetByIdAsync(puzzleId) ?? throw new InvalidOperationException("El reto semanal ya no está disponible.")
            : null;
        var puzzle = mode switch
        {
            PuzzleMode.Daily => await puzzles.GetDailyAsync(playedOn),
            PuzzleMode.Weekly => CurrentWeekly!.Puzzle,
            _ => await puzzles.GetByIdAsync(puzzleId) ?? throw new InvalidOperationException("Reto no encontrado.")
        };

        Engine = new PuzzleEngine();
        Engine.Start(puzzle, mode, playedOn);
        monetization.BeginAttempt(Engine.State!.AttemptId, puzzle.Id, referred);
        activity.Attach(Engine);
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
        monetization.BeginAttempt(Engine.State!.AttemptId, state.Puzzle.Id);
        activity.Attach(Engine);
        LastResult = null;
        LastProgressUpdate = null;
    }
}
