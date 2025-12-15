using Domain;
using VirusTotalNet.ResponseCodes;
using VirusTotalNet.Results;

namespace Infrastructure.Services.VirusScan.VirusTotal;

public class VirusTotalClient
{
    private readonly VirusTotalNet.VirusTotal _virusTotal;
    private readonly bool _simulateNetworkFailure;
    public VirusTotalClient(string apiKey, bool simulateNetworkFailure = false)
    {
        _simulateNetworkFailure = simulateNetworkFailure;
        
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("VirusTotal API key cannot be null or empty", nameof(apiKey));

        _virusTotal = new VirusTotalNet.VirusTotal(apiKey)
        {
            UseTLS = true
        };
    }
    
    public virtual async Task<VirusScanResult> Scan(byte[]? fileBytes, string? filename = null)
    {
        if (fileBytes == null)
            throw new FileNotFoundException("Empty bytes passed for virus scan.");

        if (string.IsNullOrEmpty(filename))
            filename = $"{DateTime.Now:yyyyMMddHHmmss}_scan.bin";
        
        return await ScanBytesAsync(fileBytes, filename);
    }

    protected virtual async Task<VirusScanResult> ScanBytesAsync(byte[] data, string filename = "scan.bin")
    {
        try
        {
            if (filename.Length > 255)
                throw new ArgumentException("Filename cannot be longer than 255 characters.", nameof(filename));
            
            var scanResult = await _virusTotal.ScanFileAsync(data, filename);
            await Task.Delay(3000); // wait for the scan to begin before checking results
            
            const int maxRetries = 10;
            for (var i = 0; i < maxRetries; i++)
            {
                try
                {
                    var report = await _virusTotal.GetFileReportAsync(scanResult.ScanId);

                    if (!_simulateNetworkFailure && report.ResponseCode != FileReportResponseCode.Queued)
                        return new VirusScanResult(IsScanResultSafe(report), report.Scans.Select(s => s.Key).ToList(), report.VerboseMsg);
                }
                catch
                {
                    // Scan might not be complete yet, wait and retry
                }
                
                // Exponential backoff (capped at 15 seconds)
                var delay = (int)Math.Min(Math.Pow(2, i + 1), 15);
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }
            
            // If we couldn't get results after retries, be cautious
            return new VirusScanResult(false, null,"Virus scan timed out.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VirusTotal scan failed: {ex.Message}");
            return new VirusScanResult(false, null, $"VirusTotal scan failed: {ex.Message}"); 
        }
    }
    
    protected static bool IsScanResultSafe(FileReport report) => report.Positives == 0;
}