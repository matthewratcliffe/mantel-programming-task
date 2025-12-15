using Application.Interfaces;
using Domain;

namespace Infrastructure.Services.VirusScan.Dummy;

public class DummyVirusScanService : IVirusScan
{
    public async Task<VirusScanResult> Scan(byte[] fileBytes)
    {
        await Task.Delay(80); // simulate an api call
        var scanResult = Random.Shared.NextDouble() < 0.8; // 80% chance of returning true
        return new VirusScanResult(scanResult, ["DummyEngine"], "NOT A REAL SCAN RESULT");
    }
}