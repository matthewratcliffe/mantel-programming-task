using Application.Interfaces;

namespace Application.LogParse.Base;

public class LogParseBase(IFileReader fileReader, IApplicationLifetime appLifetime)
{
    internal static string GetLogFilePath(string fileName)
    {
        return Path.Combine(Directory.GetCurrentDirectory(), fileName);
    }
    
    public async Task<byte[]> GetLogFileContents(string? fileName = null)
    {
        if (string.IsNullOrEmpty(fileName))
            fileName = "programming-task-example-data.log";
        
        var fileBytes = await fileReader.ReadAllBytes(GetLogFilePath(fileName));

        if (fileBytes.Length == 0)
        {
            Console.WriteLine("Unable to read log file.");
            appLifetime.Exit();
        }
        
        return fileBytes;
    }
}