using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Palabravo.Core.Models;
using Palabravo.Core.Services;
using Palabravo.Services;

namespace Palabravo.ViewModels;

public partial class ProfileViewModel(
    ProgressService progress,
    IPlayerAccountService accountService,
    CelebrationEffectsService effects) : ObservableObject
{
    [ObservableProperty] private string accountName = "Invitado";
    [ObservableProperty] private string accountCaption = "Protege tu progreso y conserva tu lugar en el ranking.";
    [ObservableProperty] private bool canUseGooglePlayGames;
    [ObservableProperty] private bool isGoogleLinked;
    [ObservableProperty] private bool isAccountBusy;
    [ObservableProperty] private string? accountMessage;
    [ObservableProperty] private string googleButtonText = "Continuar con Google Play Games";
    [ObservableProperty] private string rank = "Novato";
    [ObservableProperty] private string rankCaption = "5 retos para subir";
    [ObservableProperty] private double rankProgress;
    [ObservableProperty] private string currentStreak = "0";
    [ObservableProperty] private string bestStreak = "0";
    [ObservableProperty] private string resolved = "0";
    [ObservableProperty] private string perfect = "0%";
    [ObservableProperty] private string gold = "0";
    [ObservableProperty] private string silver = "0";
    [ObservableProperty] private string bronze = "0";
    [ObservableProperty] private bool soundEnabled = effects.SoundEnabled;

    public bool IsAccountIdle => !IsAccountBusy;

    public async Task RefreshAsync()
    {
        ApplyAccount(accountService.GetSnapshot());
        var player = await progress.LoadAsync();
        var rankProgress = player.RankProgress;
        Rank = rankProgress.Definition.Name;
        RankProgress = rankProgress.Fraction;
        if (rankProgress.IsPathComplete)
        {
            RankCaption = "Camino completado";
        }
        else
        {
            var remaining = rankProgress.RequiredInRank - rankProgress.CompletedInRank;
            var nextRank = RankCatalog.Next(rankProgress.Definition);
            RankCaption = nextRank is null
                ? $"{remaining} retos para dominar el camino"
                : $"{remaining} retos para subir a {nextRank.Name}";
        }
        CurrentStreak = player.CurrentStreak.ToString();
        BestStreak = player.BestStreak.ToString();
        Resolved = player.TotalResolved.ToString();
        Perfect = $"{player.PerfectPercentage}%";
        Gold = Count(player, Medal.Gold);
        Silver = Count(player, Medal.Silver);
        Bronze = Count(player, Medal.Bronze);
    }

    [RelayCommand(CanExecute = nameof(CanContinueWithGoogle))]
    private async Task ContinueWithGoogleAsync()
    {
        IsAccountBusy = true;
        AccountMessage = null;
        ContinueWithGoogleCommand.NotifyCanExecuteChanged();
        try
        {
            var result = await accountService.ContinueWithGoogleAsync();
            AccountMessage = result.Message;
            ApplyAccount(accountService.GetSnapshot());
        }
        finally
        {
            IsAccountBusy = false;
            ContinueWithGoogleCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanContinueWithGoogle() =>
        CanUseGooglePlayGames && !IsGoogleLinked && !IsAccountBusy;

    public async Task<PlayerAccountDeletionResult> DeleteAccountAsync()
    {
        IsAccountBusy = true;
        AccountMessage = null;
        try
        {
            var result = await accountService.DeleteAccountAsync();
            AccountMessage = result.Message;
            if (!result.IsSuccess)
                return result;

            await progress.ResetAsync();
            effects.ResetPreferences();
            AccountName = "Cuenta eliminada";
            AccountCaption = "La solicitud fue aceptada.";
            IsGoogleLinked = false;
            return result;
        }
        finally
        {
            IsAccountBusy = false;
        }
    }

    [RelayCommand]
    private static Task OpenDeletionPageAsync() =>
        Launcher.Default.OpenAsync(new Uri(AccountDeletionGateway.PublicDeletionPage));

    private void ApplyAccount(PlayerAccountSnapshot account)
    {
        AccountName = account.DisplayName;
        IsGoogleLinked = account.IsGoogleLinked;
        CanUseGooglePlayGames = account.CanUseGooglePlayGames;
        AccountCaption = account.IsGoogleLinked
            ? "Cuenta protegida con Google Play Games."
            : "Protege tu progreso y conserva tu lugar en el ranking.";
        GoogleButtonText = account.IsGoogleLinked
            ? "Vinculado con Google Play Games"
            : "Continuar con Google Play Games";
        ContinueWithGoogleCommand.NotifyCanExecuteChanged();
    }

    private static string Count(PlayerProgress progress, Medal medal) =>
        progress.Completions.Values.Count(x => x.BestMedal == medal).ToString();

    partial void OnSoundEnabledChanged(bool value)
    {
        effects.SoundEnabled = value;
        if (value)
            effects.PlayGroupFound();
    }

    partial void OnIsAccountBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAccountIdle));
        ContinueWithGoogleCommand.NotifyCanExecuteChanged();
    }
}
