namespace Palabravo.Core.Models;

public sealed class PuzzleDefinition
{
    public string Id { get; set; } = string.Empty;
    public int Order { get; set; }
    public int ContentVersion { get; set; } = 1;
    public RankTier Rank { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Dynamic { get; set; } = "connections";
    public List<PathQuestion> Questions { get; set; } = [];
    public string DynamicLabel => Dynamic switch { "recall" => "No te confíes", "bridge" => "Completa", "trivia" => "Trivia", "pieces" => "Combina", "intruder" => "El intruso", "visual" => "Observa", _ => "Conecta" };
    public string Instructions { get; set; } = "Encuentra cuatro conjuntos de cuatro palabras relacionadas.";
    public List<PuzzleGroup> Groups { get; set; } = [];
}

public sealed class PathQuestion
{
    public List<string> Fragments { get; set; } = [];
    public List<string> SolutionParts { get; set; } = [];
    public string Prompt { get; set; } = string.Empty;
    public List<string> Options { get; set; } = [];
    public string Answer { get; set; } = string.Empty;
    public List<string> AcceptedAnswers { get; set; } = [];
    public bool AcceptsWrittenAnswer(string value) =>
        new[] { Answer }.Concat(AcceptedAnswers).Any(answer => NormalizeAnswer(answer) == NormalizeAnswer(value));

    public static string NormalizeAnswer(string value) => value.Trim().ToLowerInvariant()
        .Normalize(System.Text.NormalizationForm.FormC)
        .Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u').Replace('ü', 'u');
    public string Explanation { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
    public string? ImageSource { get; set; }
}

public sealed class PuzzleGroup
{
    public string Category { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
    public List<string> Words { get; set; } = [];
}
