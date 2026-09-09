using Palabravo.Core.Services;

namespace Palabravo.Services;

public sealed class ReferralService(IPuzzleRepository puzzles)
{
    private string? pending;
    private bool opening;
    public void Receive(Uri uri)
    {
        pending = ReferralLink.Parse(uri);
        if (Application.Current?.Windows.FirstOrDefault()?.Page is not null)
            MainThread.BeginInvokeOnMainThread(async () => await OpenPendingAsync());
    }
    public async Task OpenPendingAsync()
    {
        if (opening || pending is not { } id || Shell.Current is null) return;
        // Do not replace a puzzle while an ad or purchase is on screen.
        if (Shell.Current.CurrentPage is Views.GamePage) return;
        opening = true;
        try
        {
            if (await puzzles.GetByIdAsync(id) is null) { pending = null; return; }
            pending = null;
            await Shell.Current.GoToAsync(nameof(Views.GamePage), new Dictionary<string, object>
            { ["puzzleId"] = id, ["mode"] = "Challenge", ["referred"] = true });
        }
        finally { opening = false; }
    }
}
