using Application.Interfaces;
using Infrastructure.Services.VirusScan.Dummy;
using Infrastructure.Services.VirusScan.VirusTotal;

namespace Infrastructure.Services.VirusScan;

public class VirusScanServiceFactory(string? apiKey) : IVirusScanServiceFactory
{
    protected readonly string? ApiKey = apiKey;

    public virtual IVirusScan Create()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            return new DummyVirusScanService();

        var client = new VirusTotalClient(ApiKey);
        return new VirusTotalScanService(client);
    }
}