using Domain;

namespace Application.Interfaces;

public interface ILogParser
{
    public List<ParsedLogLine> Parse(byte[] fileBytes);
}