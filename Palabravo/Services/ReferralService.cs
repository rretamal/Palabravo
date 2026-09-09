using Palabravo.Core.Services;
using Palabravo.Core.Models;

namespace Palabravo.Services;

public sealed class ReferralService(IPuzzleRepository puzzles, WeeklyChallengeService weeklyChallenges,
    Palabravo.Core.Monetization.IMonetizationTelemetry telemetry)
{
    private string? pending;
    private string? pendingChallenge;
    private bool pendingWeekly;
    private bool opening;
    public void Receive(Uri uri)
    {
        pending = ReferralLink.Parse(uri);
        pendingChallenge = ChallengeToken(uri);
        pendingWeekly = (uri.Scheme == "https" && uri.Host == "palabravo.app"
            && uri.AbsolutePath.Equals("/semanal", StringComparison.OrdinalIgnoreCase))
            || (uri.Scheme == "palabravo" && uri.Host.Equals("semanal", StringComparison.OrdinalIgnoreCase));
        if (pendingChallenge is not null)
            telemetry.Track("challenge_link_opened", new Dictionary<string, object> { ["source"] = "weekly_share" });
        if (Application.Current?.Windows.FirstOrDefault()?.Page is not null)
            MainThread.BeginInvokeOnMainThread(async () => await OpenPendingAsync());
    }
    public async Task OpenPendingAsync()
    {
        if (opening || Shell.Current is null || (pending is null && pendingChallenge is null && !pendingWeekly)) return;
        // Do not replace a puzzle while an ad or purchase is on screen.
        if (Shell.Current.CurrentPage is Views.GamePage) return;
        opening = true;
        try
        {
            if (pendingChallenge is { } token)
            {
                var invite = await weeklyChallenges.ResolveInviteAsync(token);
                var weekly = invite is null ? null : await weeklyChallenges.GetByIdAsync(invite.WeeklyId);
                if (weekly is null) { pendingChallenge = null; return; }
                pendingChallenge = null;
                await OpenGameAsync(weekly.Id, PuzzleMode.Weekly, true);
                return;
            }
            if (pendingWeekly)
            {
                var weekly = await weeklyChallenges.GetCurrentAsync();
                pendingWeekly = false;
                if (weekly is not null) await OpenGameAsync(weekly.Id, PuzzleMode.Weekly, false);
                return;
            }
            if (pending is not { } id || await puzzles.GetByIdAsync(id) is null) { pending = null; return; }
            pending = null;
            await OpenGameAsync(id, PuzzleMode.Challenge, true);
        }
        finally { opening = false; }
    }

    private static Task OpenGameAsync(string id, PuzzleMode mode, bool referred) =>
        Shell.Current.GoToAsync(nameof(Views.GamePage), new Dictionary<string, object>
        { ["puzzleId"] = id, ["mode"] = mode.ToString(), ["referred"] = referred });

    private static string? ChallengeToken(Uri uri)
    {
        if (uri.Scheme == "palabravo" && uri.Host.Equals("challenge", StringComparison.OrdinalIgnoreCase))
        {
            var customToken = uri.AbsolutePath.Trim('/').ToUpperInvariant();
            return System.Text.RegularExpressions.Regex.IsMatch(customToken, "^[A-Z0-9]{6}$") ? customToken : null;
        }
        if (uri.Scheme != "https" || uri.Host != "palabravo.app" || !uri.IsDefaultPort
            || !uri.AbsolutePath.StartsWith("/challenge/", StringComparison.OrdinalIgnoreCase)) return null;
        var token = uri.AbsolutePath[11..].Trim('/').ToUpperInvariant();
        return System.Text.RegularExpressions.Regex.IsMatch(token, "^[A-Z0-9]{6}$") ? token : null;
    }
}
