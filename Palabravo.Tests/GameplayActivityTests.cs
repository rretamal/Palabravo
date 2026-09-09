using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Tests;

public sealed class GameplayActivityTests
{
    [Fact]
    public void New_attempt_inherits_a_pending_consent_pause()
    {
        var activity = new GameplayActivity();
        activity.SetForeground(true); activity.SetVisible(true);
        using (activity.Pause())
        {
            var engine = Start();
            activity.Attach(engine);
            Assert.False(engine.IsTiming);
            Assert.False(activity.IsActive);
        }
        Assert.True(activity.IsActive);
    }

    [Fact]
    public void Closing_a_modal_in_background_does_not_resume_the_clock()
    {
        var activity = new GameplayActivity();
        var engine = Start(); activity.Attach(engine);
        activity.SetVisible(true); activity.SetForeground(true);
        Assert.True(engine.IsTiming);
        var modal = activity.Pause();
        activity.SetForeground(false);
        modal.Dispose(); modal.Dispose();
        Assert.False(engine.IsTiming);
        activity.SetForeground(true);
        Assert.True(engine.IsTiming);
        activity.SetVisible(false);
        Assert.False(engine.IsTiming);
    }

    [Fact]
    public void Overlapping_pauses_apply_to_the_replacement_engine_until_both_close()
    {
        var activity = new GameplayActivity();
        activity.SetVisible(true); activity.SetForeground(true);
        activity.Attach(Start());
        var first = activity.Pause(); var second = activity.Pause();
        var replacement = Start(); activity.Attach(replacement);
        first.Dispose();
        Assert.False(replacement.IsTiming);
        second.Dispose();
        Assert.True(replacement.IsTiming);
    }

    private static PuzzleEngine Start()
    {
        var engine = new PuzzleEngine();
        engine.Start(PuzzleEngineTests.CreatePuzzle(), PuzzleMode.Challenge, new(2026, 9, 9));
        return engine;
    }
}
