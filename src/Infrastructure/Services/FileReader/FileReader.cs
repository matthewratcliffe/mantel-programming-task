using System.Security.Cryptography;
using Application.Interfaces;

namespace Infrastructure.Services.FileReader;

public class FileReader(IVirusScanServiceFactory virusScanFactory) : IFileReader
{
    private string _currentFileHash = string.Empty;
    private byte[] _currentFileBytes = [];
    
    public async Task<byte[]> ReadAllBytes(string path)
    {
        Console.WriteLine($"Reading file: {path}");
        var fileBytes = await File.ReadAllBytesAsync(path);
        var fileHash = GetSha256Hash(fileBytes);

        if (!string.IsNullOrEmpty(_currentFileHash) && fileHash.Equals(_currentFileHash))
        {
            Console.WriteLine("Previously Passed, No Changes Detected ✓");
            return _currentFileBytes;
        }
        
        var isFileClean = await FileDoesNotContainVirusOrMalware(fileBytes);

        if (!isFileClean)
        {
            return [];
        }
        
        _currentFileHash = fileHash;
        _currentFileBytes = fileBytes;
        
        return fileBytes;
    }

    private async Task<bool> FileDoesNotContainVirusOrMalware(byte[] fileBytes)
    {
        Console.Write("Scanning for Viruses, Please Wait .... ");
        
        var factory = virusScanFactory.Create();        
        var virusScanResult = await factory.Scan(fileBytes);
        
        Console.WriteLine($"Scan completed with: {string.Join(",", virusScanResult.EnginesUsed ?? ["N/A"])}");
        
        Console.WriteLine(virusScanResult.IsClean ? "Passed ✓" : "Failed ✗");
        
        return virusScanResult.IsClean;
    }

    internal static string GetSha256Hash(byte[] fileBytes)
    {
        var hashBytes = SHA256.HashData(fileBytes);
        return Convert.ToHexStringLower(hashBytes);
    }
}