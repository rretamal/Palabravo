namespace Palabravo.Core.Models;

public sealed class PuzzleDefinition
{
    public string Id { get; set; } = string.Empty;
    public int Order { get; set; }
    public int ContentVersion { get; set; } = 1;
    public RankTier Rank { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<PuzzleGroup> Groups { get; set; } = [];
}

public sealed class PuzzleGroup
{
    public string Category { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
    public List<string> Words { get; set; } = [];
}
