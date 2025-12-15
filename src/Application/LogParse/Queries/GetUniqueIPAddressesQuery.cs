using Application.Interfaces;
using Application.LogParse.Base;
using MediatR;

namespace Application.LogParse.Queries;

public class GetUniqueIpAddressesQuery : IRequest<IEnumerable<string?>>;

public class GetUniqueIpAddressesQueryHandler(ILogParser logParser, IFileReader fileReader, IApplicationLifetime appLifetime)
    : LogParseBase(fileReader, appLifetime), IRequestHandler<GetUniqueIpAddressesQuery, IEnumerable<string?>>
{
    public async Task<IEnumerable<string?>> Handle(GetUniqueIpAddressesQuery request, CancellationToken cancellationToken)
    {
        var results = logParser.Parse(await GetLogFileContents());
        
        if (results.Count == 0)
            throw new Exception("No log entries found");

        return results
            .Where(l => l.Key.Equals("ip"))
            .Select(l => l.Value)
            .Distinct();
    }
}