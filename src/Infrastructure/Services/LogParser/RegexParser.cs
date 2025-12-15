using System.Text;
using System.Text.RegularExpressions;
using Application.Interfaces;
using Domain;

namespace Infrastructure.Services.LogParser;

public class RegexParserService : ILogParser
{
    private readonly Regex _regex = new(
        @"^(?<ip>\S+)\s+" +
        @"(?<ident>\S+)\s+" +
        @"(?<authuser>\S+)\s+" +
        @"\[(?<timestamp>[^\]]+)\]\s+" +
        @"""(?<method>\S+)\s+(?<path>[^""]+?)\s+(?<protocol>\S+)""\s+" +
        @"(?<status>\d{3})\s+" +
        @"(?<bytes>\S+)\s+" +
        @"""(?<referrer>[^""]*)""\s+" +
        @"""(?<agent>[^""]*)""",
        RegexOptions.Compiled);
    
    public List<ParsedLogLine> Parse(byte[] fileBytes)
    {
        var log = Encoding.UTF8.GetString(fileBytes);
        var result = new List<ParsedLogLine>();
        var lines = log.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var match = _regex.Match(line);
            var lineNum = i + 1;

            if (!match.Success)
            {
                result.Add(new ParsedLogLine
                {
                    LineNumber = lineNum,
                    Key = "raw",
                    Value = line
                });
                continue;
            }

            result.AddRange(from groupName in _regex.GetGroupNames()
                where !int.TryParse(groupName, out _)
                select new ParsedLogLine
                {
                    LineNumber = lineNum, 
                    Key = groupName, 
                    Value = match.Groups[groupName].Value
                });
        }

        return result;
    }
}