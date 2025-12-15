using Infrastructure.Services.VirusScan.VirusTotal;

namespace Test.UnitTests.Integration;

public class VirusTotalClientTests
{
    private static readonly string? ValidApiKey = Environment.GetEnvironmentVariable("VIRUSTOTAL_API_KEY") ?? throw new Exception("VIRUSTOTAL_API_KEY environment variable not set");
    private readonly VirusTotalClient _virusTotalClient = new(ValidApiKey!);

    [Test]
    public async Task VirusTotal_MalwarePayload_ShouldReturnError()
    {
        // This is the EICAR test virus file - it contains NO malicious payload
        // for more information see https://www.eicar.org/
        
        // Arrange
        var fileBytes = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*"u8.ToArray();
        
        // Act
        var result = await _virusTotalClient.Scan(fileBytes);
        
        // Assert
        Assert.That(result.IsClean, Is.False);
    }

    [Test]
    public async Task VirusTotal_SafePayload_ShouldReturnSafe()
    {
        // Arrange
        var fileBytes = "MZ"u8.ToArray();
        
        // Act
        var result = await _virusTotalClient.Scan(fileBytes);
        
        // Assert
        Assert.That(result.IsClean, Is.True);
    }
    
    [Test]
    public void VirusTotal_NoPayload_ShouldThrow()
    {
        // Arrange, Act & Assert
        Assert.ThrowsAsync<FileNotFoundException>(() => _virusTotalClient.Scan(null!));
    }
    
    [Test]
    public async Task VirusTotal_SafePayload_ShouldReturnKnownEngines()
    {
        // Arrange
        var fileBytes = "MZ"u8.ToArray();
        
        // Act
        var result = await _virusTotalClient.Scan(fileBytes);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.EnginesUsed, Is.Not.Null);
            Assert.That(result.EnginesUsed!, Is.Not.Empty);
            Assert.That(result.EnginesUsed, Contains.Item("AVG"));
            Assert.That(result.EnginesUsed, Contains.Item("Avast"));
        });
    }
    
    [Test]
    public async Task ScanBytesAsync_WhenAllRetriesExhausted_ReturnsTimeoutResultWithCorrectProperties()
    {
        // Create a testable client that mocks the VirusTotal API
        var client = new VirusTotalClient(ValidApiKey, true); // Using small retry count and delays for faster tests
    
        // Act
        var result = await client.Scan([1, 2, 3]);
    
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsClean, Is.False, "Timeout result should mark file as not clean (unsafe)");
            Assert.That(result.EnginesUsed, Is.Null, "Timeout result should have null engines used");
            Assert.That(result.Message, Is.EqualTo("Virus scan timed out."), "Timeout result should have correct message");
        });
    }
    
    [Test]
    public async Task ScanBytesAsync_WithNoExternalConnectivity_ReturnsTimeoutResult()
    {
        // Arrange
        // Create a client with an invalid API endpoint to simulate connectivity issues
        var client = new VirusTotalClient(ValidApiKey, true);
        var testData = new byte[] { 0x01, 0x02, 0x03 };
        
        // Act
        // This should trigger the timeout flow since it can't connect to the API
        var result = await client.Scan(testData);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsClean, Is.False, "Should return unsafe result when timeout occurs");
            Assert.That(result.EnginesUsed, Is.Null, "Should not have any engines used on timeout");
            Assert.That(result.Message, Is.EqualTo("Virus scan timed out."), "Should return the timeout message");
        });
    }
}