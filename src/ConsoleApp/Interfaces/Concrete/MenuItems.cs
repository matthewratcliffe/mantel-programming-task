using Application.Interfaces;
using Application.LogParse.Queries;
using MediatR;

namespace ConsoleApp.Interfaces.Concrete;

public class MenuItems(IMediator mediator, IApplicationLifetime appLifetime) : IMenuItems
{
    public async Task HandleUniqueIpAddresses()
    {
        var results = await mediator.Send(new GetUniqueIpAddressesQuery(), CancellationToken.None);
        
        Console.WriteLine($"There are {results.Count()} unique IP addresses in the log file");
    }

    public async Task HandleTopXMostActiveIps(int count = 3)
    {
        var rankGroups = await mediator.Send(new GetTopMostActiveIpAddressesQuery { Count = count }, CancellationToken.None);

        Console.WriteLine("Most active IP addresses:");
        foreach (var group in rankGroups)
        {
            Console.WriteLine($"  {Ordinal(group.Rank)} ({group.HitCount} hits): {string.Join(", ", group.Items)}");
        }
    }
    
    public async Task HandleTopXVisitedUrls(int count = 3)
    {
        var rankGroups = await mediator.Send(new GetTopVisitedUrlsQuery { Count = count }, CancellationToken.None);

        Console.WriteLine("Most visited URLs:");
        foreach (var group in rankGroups)
        {
            Console.WriteLine($"  {Ordinal(group.Rank)} ({group.HitCount} hits): {string.Join(", ", group.Items)}");
        }
    }

    public void HandleExit()
    {
        Console.WriteLine("Exiting program...");
        appLifetime.Exit();
    }

    private static string Ordinal(int number)
    {
        if (number % 100 is 11 or 12 or 13) return $"{number}th";
        return (number % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }
}