using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.Tests;

public sealed class RankingParticipationTests
{
    [Fact]
    public void NewPlayerMustCompleteAGameEvenAfterTutorial()
    {
        var progress = new PlayerProgress { TutorialSeen = true };
        Assert.False(RankingParticipation.CanViewPath(progress));
        Assert.False(RankingParticipation.CanViewWeekly(progress, "this-week"));
    }

    [Theory]
    [InlineData(PuzzleMode.Challenge)]
    [InlineData(PuzzleMode.Daily)]
    [InlineData(PuzzleMode.Weekly)]
    public void AnyCompletedGameUnlocksGeneralRanking(PuzzleMode mode)
    {
        var progress = new PlayerProgress();
        progress.Completions["saved"] = new CompletionRecord { Mode = mode, BestMedal = Medal.Bronze };
        Assert.True(RankingParticipation.CanViewPath(progress));
        Assert.False(RankingParticipation.CanViewWeekly(progress, "this-week"));
    }

    [Fact]
    public void WeeklyAccessRequiresCurrentCompetitionAndResetsWithNextPuzzle()
    {
        var progress = new PlayerProgress();
        progress.Completions["weekly:last-week"] = new CompletionRecord { Mode = PuzzleMode.Weekly };
        Assert.False(RankingParticipation.CanViewWeekly(progress, "this-week"));
        progress.Completions["weekly:this-week"] = new CompletionRecord
        { Mode = PuzzleMode.Weekly, BestMedal = Medal.Bronze };
        Assert.True(RankingParticipation.CanViewWeekly(progress, "this-week"));
        Assert.False(RankingParticipation.CanViewWeekly(progress, "next-week"));
    }
}
