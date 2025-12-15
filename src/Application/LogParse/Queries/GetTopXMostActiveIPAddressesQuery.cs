using Application.Interfaces;
using Application.LogParse.Base;
using Application.LogParse.Models;
using MediatR;

namespace Application.LogParse.Queries;

/// <summary>
/// Query to get the top most active IP addresses from log content
/// </summary>
public class GetTopMostActiveIpAddressesQuery : IRequest<IEnumerable<RankGroup>>
{
    /// <summary>
    /// The number of top active IP addresses to return (default: 3)
    /// </summary>
    public int Count { get; init; } = 3;
}

/// <summary>
/// Handler for getting top most active IP addresses from log content
/// </summary>
public class GetTopMostActiveIpAddressesQueryHandler(ILogParser logParser, IFileReader fileReader, IApplicationLifetime appLifetime) : LogParseBase(fileReader, appLifetime), IRequestHandler<GetTopMostActiveIpAddressesQuery, IEnumerable<RankGroup>>
{
    public async Task<IEnumerable<RankGroup>> Handle(GetTopMostActiveIpAddressesQuery request, CancellationToken cancellationToken)
    {
        var results = logParser.Parse(await GetLogFileContents());
        
        if (results.Count == 0)
            throw new Exception("No log entries found");

        var frequencies = results
            .Where(l => l.Key.Equals("ip"))
            .GroupBy(l => l.Value)
            .Select(g => new { Item = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Item)
            .ToList();

        if (frequencies.Count == 0)
            return [];

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