using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Palabravo.Core.Models;
using Palabravo.Services;

namespace Palabravo.ViewModels;

public partial class ResultViewModel(GameCoordinator coordinator) : ObservableObject
{
    public const string AndroidInstallUrl = "https://play.google.com/store/apps/details?id=com.palabravo.app";
    private static readonly string[] GroupColors = ["#FCE4DE", "#FFF0C8", "#DDEFE9", "#E5E9F5"];

    public ObservableCollection<ResultGroupViewModel> SolutionGroups { get; } = [];

    [ObservableProperty] private bool isSuccess;
    [ObservableProperty] private string eyebrow = "RETO COMPLETADO";
    [ObservableProperty] private string title = "¡Conexiones encontradas!";
    [ObservableProperty] private string medalIcon = "🥇";
    [ObservableProperty] private string medalText = "Medalla de oro";
    [ObservableProperty] private string errors = "0";
    [ObservableProperty] private string hints = "0";
    [ObservableProperty] private string time = "00:00";
    [ObservableProperty] private bool solutionVisible;
    [ObservableProperty] private bool showSolutionButton;
    [ObservableProperty] private bool isRankUp;
    [ObservableProperty] private string rankUpIcon = "✦";
    [ObservableProperty] private string rankUpTitle = "Nuevo rango";
    [ObservableProperty] private string rankUpText = string.Empty;
    [ObservableProperty] private Color rankUpColor = Color.FromArgb("#EB5B43");
    [ObservableProperty] private Color rankUpBackgroundColor = Color.FromArgb("#FCE4DE");
    [ObservableProperty] private string shareHeadline = "Te reto a encontrar las cuatro conexiones";
    [ObservableProperty] private string shareResultText = string.Empty;

    public void Refresh()
    {
        var result = coordinator.LastResult;
        if (result is null)
            return;

        IsSuccess = result.IsSuccess;
        Eyebrow = result.Mode == PuzzleMode.Daily ? "RETO DIARIO" : "CAMINO PALABRAVO";
        Title = result.IsSuccess ? "¡Conexiones encontradas!" : "Casi lo tienes";
        MedalIcon = result.Medal switch
        {
            Medal.Gold => "🥇",
            Medal.Silver => "🥈",
            Medal.Bronze => "🥉",
            _ => "✦"
        };
        MedalText = result.Medal switch
        {
            Medal.Gold => "Medalla de oro",
            Medal.Silver => "Medalla de plata",
            Medal.Bronze => "Medalla de bronce",
            _ => "Inténtalo otra vez"
        };
        Errors = result.Errors.ToString();
        Hints = result.HintsUsed.ToString();
        Time = $"{(int)result.Elapsed.TotalMinutes:00}:{result.Elapsed.Seconds:00}";
        ShareHeadline = result.Mode == PuzzleMode.Daily
            ? $"Reto diario · {result.PlayedOn:dd/MM}"
            : result.PuzzleTitle;
        ShareResultText = $"{MedalIcon} {result.Errors} errores · {result.HintsUsed} pistas · {Time}";
        SolutionVisible = result.IsSuccess || result.SolutionRequested;
        ShowSolutionButton = !result.IsSuccess && !result.SolutionRequested;
        var progressUpdate = coordinator.LastProgressUpdate;
        IsRankUp = progressUpdate?.RankChanged == true;
        if (IsRankUp && progressUpdate is not null)
        {
            RankUpIcon = progressUpdate.CurrentRank.Icon;
            RankUpTitle = $"¡Ahora eres {progressUpdate.CurrentRank.Name}!";
            RankUpText = $"Completaste el rango {progressUpdate.PreviousRank.Name}. El siguiente tramo ya está abierto.";
            RankUpColor = Color.FromArgb(progressUpdate.CurrentRank.AccentColor);
            RankUpBackgroundColor = Color.FromArgb(progressUpdate.CurrentRank.SoftColor);
        }
        SolutionGroups.Clear();
        for (var i = 0; i < result.SolutionGroups.Count; i++)
        {
            var group = result.SolutionGroups[i];
            SolutionGroups.Add(new ResultGroupViewModel(
                group.Category,
                string.Join(" · ", group.Words),
                GroupColors[i % GroupColors.Length]));
        }
    }

    [RelayCommand]
    private void ShowSolution()
    {
        SolutionVisible = true;
        ShowSolutionButton = false;
    }

    [RelayCommand]
    private async Task ReplayAsync()
    {
        coordinator.Restart();
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task GoHomeAsync() => Shell.Current.GoToAsync("//home");

    public string BuildShareText()
    {
        var result = coordinator.LastResult;
        if (result is null)
            return string.Empty;

        var label = result.Mode == PuzzleMode.Daily
            ? $"el reto diario del {result.PlayedOn:dd/MM}"
            : $"«{result.PuzzleTitle}»";
        return $"🟧 PALABRAVO · Te reto a resolver {label}\n\n{ShareResultText}\n\n¿Puedes encontrar las cuatro conexiones?\n\n📲 Instálalo aquí:\n{AndroidInstallUrl}";
    }

    public Task ShareTextAsync() => Share.Default.RequestAsync(
        new ShareTextRequest(BuildShareText(), "Te reto en Palabravo"));
}

public sealed record ResultGroupViewModel(string Category, string Words, string BackgroundColor);
