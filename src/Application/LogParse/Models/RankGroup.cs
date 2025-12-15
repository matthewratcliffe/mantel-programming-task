namespace Application.LogParse.Models;

public class RankGroup
{
    public int Rank { get; init; }
    public int HitCount { get; init; }
    public IEnumerable<string?> Items { get; init; } = [];
}
