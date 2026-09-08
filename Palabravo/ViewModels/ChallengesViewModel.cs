using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Palabravo.Core.Models;
using Palabravo.Core.Services;

namespace Palabravo.ViewModels;

public partial class ChallengesViewModel(IPuzzleRepository puzzles, ProgressService progress) : ObservableObject
{
    public ObservableCollection<RankSectionViewModel> RankSections { get; } = [];

    [ObservableProperty] private string rankName = "Novato";
    [ObservableProperty] private string rankProgressText = "0/5";
    [ObservableProperty] private string progressPercentText = "0%";
    [ObservableProperty] private string rankCaption = "Completa 5 retos para subir a Ágil";
    [ObservableProperty] private double generalProgress;
    [ObservableProperty] private string goldCount = "0";
    [ObservableProperty] private string silverCount = "0";
    [ObservableProperty] private string bronzeCount = "0";
    [ObservableProperty] private int completedCount;
    [ObservableProperty] private int currentRankIndex;
    [ObservableProperty] private string continueText = "Comenzar · Reto 1";

    private ChallengeNodeViewModel? currentChallenge;

    public async Task RefreshAsync()
    {
        var player = await progress.LoadAsync();
        var all = (await puzzles.GetAllAsync()).OrderBy(puzzle => puzzle.Order).ToList();
        var challengeCompletions = player.Completions.Values
            .Where(completion => completion.Mode == PuzzleMode.Challenge)
            .ToList();
        CompletedCount = challengeCompletions.Count;
        var rankProgress = RankCatalog.ForCompleted(CompletedCount);

        var nodes = all.Select((puzzle, index) =>
        {
            player.Completions.TryGetValue($"challenge:{puzzle.Id}", out var completion);
            return new ChallengeNodeViewModel(
                puzzle,
                index % RankCatalog.ChallengesPerRank,
                progress.IsChallengeUnlocked(puzzle, all),
                completion);
        }).ToList();

        currentChallenge = nodes.FirstOrDefault(node => !node.IsLocked && !node.IsCompleted)
            ?? nodes.LastOrDefault(node => !node.IsLocked);
        if (currentChallenge is not null)
            currentChallenge.IsCurrent = !rankProgress.IsPathComplete;

        RankSections.Clear();
        foreach (var definition in RankCatalog.All)
        {
            var rankNodes = nodes.Where(node => node.Rank == definition.Tier).ToList();
            RankSections.Add(new RankSectionViewModel(
                definition,
                rankNodes,
                definition.Tier == rankProgress.Definition.Tier,
                rankNodes.All(node => node.IsLocked)));
        }

        RankName = rankProgress.Definition.Name;
        RankProgressText = $"{rankProgress.CompletedInRank}/{rankProgress.RequiredInRank}";
        CurrentRankIndex = rankProgress.Definition.Index;
        GeneralProgress = CompletedCount / (double)RankCatalog.TotalChallenges;
        ProgressPercentText = $"{(int)Math.Round(GeneralProgress * 100)}%";
        RankCaption = BuildRankCaption(rankProgress);
        GoldCount = challengeCompletions.Count(completion => completion.BestMedal == Medal.Gold).ToString();
        SilverCount = challengeCompletions.Count(completion => completion.BestMedal == Medal.Silver).ToString();
        BronzeCount = challengeCompletions.Count(completion => completion.BestMedal == Medal.Bronze).ToString();
        ContinueText = rankProgress.IsPathComplete
            ? "Repetir · Reto 30"
            : CompletedCount == 0
                ? "Comenzar · Reto 1"
                : $"Continuar · Reto {currentChallenge?.Number}";
    }

    [RelayCommand]
    private Task PlayAsync(ChallengeNodeViewModel? challenge) =>
        challenge is null || challenge.IsLocked ? Task.CompletedTask : OpenChallengeAsync(challenge);

    [RelayCommand]
    private Task ContinueAsync() =>
        currentChallenge is null ? Task.CompletedTask : OpenChallengeAsync(currentChallenge);

    private static string BuildRankCaption(RankProgressSnapshot progress)
    {
        if (progress.IsPathComplete)
            return "¡Camino completado!";

        var remaining = progress.RequiredInRank - progress.CompletedInRank;
        var challengeLabel = remaining == 1 ? "reto" : "retos";
        var next = RankCatalog.Next(progress.Definition);
        return next is null
            ? $"Completa {remaining} {challengeLabel} para dominar el camino"
            : $"Completa {remaining} {challengeLabel} para subir a {next.Name}";
    }

    private static Task OpenChallengeAsync(ChallengeNodeViewModel challenge) =>
        Shell.Current.GoToAsync(nameof(Views.GamePage), new Dictionary<string, object>
        {
            ["puzzleId"] = challenge.Id,
            ["mode"] = PuzzleMode.Challenge.ToString()
        });
}

public sealed class RankSectionViewModel
{
    public RankSectionViewModel(
        RankDefinition definition,
        IReadOnlyList<ChallengeNodeViewModel> nodes,
        bool isCurrent,
        bool isLocked)
    {
        Definition = definition;
        Nodes = nodes;
        IsCurrent = isCurrent;
        IsLocked = isLocked;
    }

    public RankDefinition Definition { get; }
    public IReadOnlyList<ChallengeNodeViewModel> Nodes { get; }
    public string Name => Definition.Name;
    public string Icon => Definition.Icon;
    public Color AccentColor => Color.FromArgb(Definition.AccentColor);
    public Color SoftColor => Color.FromArgb(Definition.SoftColor);
    public bool IsCurrent { get; }
    public bool IsLocked { get; }
    public double Opacity => IsLocked ? 0.6 : 1;
    public int CompletedCount => Nodes.Count(node => node.IsCompleted);
    public string ProgressText => $"{CompletedCount}/{RankCatalog.ChallengesPerRank}";
    public string StatusText => CompletedCount == RankCatalog.ChallengesPerRank
        ? "Rango completado"
        : IsLocked ? "Completa el rango anterior" : IsCurrent ? "Tu rango actual" : "En progreso";
}

public sealed class ChallengeNodeViewModel
{
    private static readonly double[] VerticalPositions = [0.08, 0.29, 0.50, 0.71, 0.92];

    public ChallengeNodeViewModel(
        PuzzleDefinition puzzle,
        int indexInRank,
        bool isUnlocked,
        CompletionRecord? completion)
    {
        Id = puzzle.Id;
        Number = puzzle.Order.ToString();
        Title = puzzle.Title;
        Difficulty = puzzle.Difficulty;
        Rank = puzzle.Rank;
        AccentColor = Color.FromArgb(RankCatalog.ForChallenge(puzzle.Order).AccentColor);
        IsLocked = !isUnlocked;
        IsCompleted = completion is not null;
        Medal = completion?.BestMedal ?? Medal.None;
        var x = indexInRank % 2 == 0 ? 0.20 : 0.80;
        LayoutBounds = new Rect(x, VerticalPositions[indexInRank], 132, 122);
    }

    public string Id { get; }
    public string Number { get; }
    public string Title { get; }
    public string Difficulty { get; }
    public RankTier Rank { get; }
    public Color AccentColor { get; }
    public bool IsLocked { get; }
    public bool IsCompleted { get; }
    public bool IsCurrent { get; set; }
    public Medal Medal { get; }
    public Rect LayoutBounds { get; }

    public string Icon => IsLocked ? "🔒" : Medal switch
    {
        Medal.Gold => "🥇",
        Medal.Silver => "🥈",
        Medal.Bronze => "🥉",
        _ => "★"
    };

    public string StateText => IsLocked ? "Bloqueado" : IsCompleted ? Medal switch
    {
        Medal.Gold => "Oro",
        Medal.Silver => "Plata",
        Medal.Bronze => "Bronce",
        _ => "Completado"
    } : IsCurrent ? "Reto actual" : Difficulty;

    public string SemanticDescription => IsLocked
        ? $"Reto {Number}, {Title}, bloqueado"
        : IsCompleted
            ? $"Reto {Number}, {Title}, completado con medalla {StateText}"
            : $"Reto {Number}, {Title}, {StateText}";

    public Color BorderColor => IsLocked ? Color.FromArgb("#E2DBD2")
        : IsCurrent ? AccentColor
        : Medal switch
        {
            Medal.Gold => Color.FromArgb("#E0A21E"),
            Medal.Silver => Color.FromArgb("#90999F"),
            Medal.Bronze => Color.FromArgb("#B96F42"),
            _ => Color.FromArgb("#59B7A4")
        };

    public Color NodeColor => IsCurrent ? AccentColor : Color.FromArgb("#FFFDFC");
    public Color NumberColor => IsCurrent ? Colors.White : IsLocked ? Color.FromArgb("#8C8781") : Color.FromArgb("#242322");
    public double Opacity => IsLocked ? 0.62 : 1;
    public double BorderThickness => IsCurrent ? 4 : 2;
}
