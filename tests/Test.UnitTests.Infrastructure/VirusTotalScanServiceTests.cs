using Domain;
using Infrastructure.Services.VirusScan.VirusTotal;
using Moq;

namespace Test.UnitTests.Infrastructure;

[TestFixture]
public class VirusTotalScanServiceTests
{
    // Note: This is NOT a real API key. It is only used for testing purposes.
    private const string ValidApiKey = "998fb43afb2c4e0bb73e5fa50a04ad9000003605XX68e6f4c86ba5119e46c424";
    
    private Mock<VirusTotalClient> _mockVirusTotalClient;
    private VirusTotalScanService _virusTotalScanService;

    [SetUp]
    public void Setup()
    {
        // Create a mock of the VirusTotalClient
        _mockVirusTotalClient = new Mock<VirusTotalClient>(ValidApiKey, false);
            
        // Create the service with the mocked client
        _virusTotalScanService = new VirusTotalScanService(_mockVirusTotalClient.Object);
    }

    [Test]
    public async Task Scan_ShouldDelegateToVirusTotalClient()
    {
        // Arrange
        var fileBytes = new byte[] { 0x01, 0x02, 0x03 };
        var expectedResult = new VirusScanResult(true, ["Engine1"], "File is clean");
            
        // Setup the mock to return the expected result
        _mockVirusTotalClient
            .Setup(c => c.Scan(It.IsAny<byte[]>(), It.IsAny<string>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _virusTotalScanService.Scan(fileBytes);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(result.IsClean, Is.True);
            Assert.That(result.EnginesUsed, Has.Count.EqualTo(1));
            Assert.That(result.EnginesUsed![0], Is.EqualTo("Engine1"));
            Assert.That(result.Message, Is.EqualTo("File is clean"));
        });
            
        // Verify the client's Scan method was called with the correct file bytes
        _mockVirusTotalClient.Verify(c => c.Scan(fileBytes, null), Times.Once);
    }

    [Test]
    public async Task Scan_ShouldReturnResultFromClient_WhenFileIsNotClean()
    {
        // Arrange
        var fileBytes = new byte[] { 0x01, 0x02, 0x03 };
        var expectedResult = new VirusScanResult(false, ["Engine1"], "File contains malware");
            
        // Setup the mock to return the expected result
        _mockVirusTotalClient
            .Setup(c => c.Scan(It.IsAny<byte[]>(), null))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _virusTotalScanService.Scan(fileBytes);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(result.IsClean, Is.False);
            Assert.That(result.Message, Is.EqualTo("File contains malware"));
        });
            
        // Verify the client's Scan method was called
        _mockVirusTotalClient.Verify(c => c.Scan(fileBytes, null), Times.Once);
    }
        
    [Test]
    public void Scan_ShouldPropagateExceptions_FromClient()
    {
        // Arrange
        var fileBytes = new byte[] { 0x01, 0x02, 0x03 };
        var expectedException = new Exception("API error");
            
        // Setup the mock to throw an exception
        _mockVirusTotalClient
            .Setup(c => c.Scan(It.IsAny<byte[]>(), null))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(async () => 
            await _virusTotalScanService.Scan(fileBytes));
            
        Assert.That(exception, Is.SameAs(expectedException));
    }
}