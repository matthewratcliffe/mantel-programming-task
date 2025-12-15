using Application.Interfaces;
using Application.LogParse.Base;
using Application.LogParse.Models;
using MediatR;

namespace Application.LogParse.Queries;

/// <summary>
/// Query to get the top most visited URLs from log content
/// </summary>
public class GetTopVisitedUrlsQuery : IRequest<IEnumerable<RankGroup>>
{
    /// <summary>
    /// The number of top URLs to return (default: 3)
    /// </summary>
    public int Count { get; init; } = 3;
}

/// <summary>
/// Handler for getting top visited URLs from log content
/// </summary>
public class GetTopVisitedUrlsQueryHandler(ILogParser logParser, IFileReader fileReader, IApplicationLifetime appLifetime)
    : LogParseBase(fileReader, appLifetime), IRequestHandler<GetTopVisitedUrlsQuery, IEnumerable<RankGroup>>
{
    public async Task<IEnumerable<RankGroup>> Handle(GetTopVisitedUrlsQuery request, CancellationToken cancellationToken)
    {
        var results = logParser.Parse(await GetLogFileContents());
        
        if (results.Count == 0)
            throw new Exception("No log entries found");

        // Frequency map of path -> hits
        var frequencies = results
            .Where(l => l.Key.Equals("path"))
            .GroupBy(l => l.Value)
            .Select(g => new { Item = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Item)
            .ToList();

        if (frequencies.Count == 0)
            return [];

        // Group by equal counts to support ties and take by place (rank) count
        var groupsByCount = frequencies
            .GroupBy(x => x.Count)
            .OrderByDescending(g => g.Key)
            .ToList();

        var rankGroups = new List<RankGroup>();
        var rank = 1;
        foreach (var group in groupsByCount.TakeWhile(_ => rank <= request.Count))
        {
            rankGroups.Add(new RankGroup
            {
                Rank = rank,
                HitCount = group.Key,
                Items = group.Select(x => x.Item)
            });

            rank++;
        }

        return rankGroups;
    }
}