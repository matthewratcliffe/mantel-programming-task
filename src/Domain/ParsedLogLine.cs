using System.Diagnostics.CodeAnalysis;

namespace Domain;

public record ParsedLogLine
{
    public ParsedLogLine()
    {
    }

    [SetsRequiredMembers]
    public ParsedLogLine(int lineNumber, string key, string? value)
    {
        LineNumber = lineNumber;
        Key = key;
        Value = value;
    }

    public int LineNumber { get; init; }
    public required string Key { get; init; }
    public string? Value { get; init; }
}