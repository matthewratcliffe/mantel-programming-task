using System.Diagnostics.CodeAnalysis;
using Domain;
using Infrastructure.Services.VirusScan.VirusTotal;
using Moq;
using Moq.Protected;
using VirusTotalNet.Results;

namespace Test.UnitTests.Infrastructure;

[TestFixture]
public class VirusTotalClientTests
{
    // Note: This is NOT a real API key. It is only used for testing purposes.
    private const string ValidApiKey = "998fb43afb2c4e0bb73e5fa50a04ad9000003605XX68e6f4c86ba5119e46c424";
        
    // Create a test-specific subclass to access the protected method
    private class TestableVirusTotalClient(string apiKey) : VirusTotalClient(apiKey)
    {
        // Public wrapper for the protected method
        public static bool PublicIsScanResultSafe(FileReport report)
        {
            return IsScanResultSafe(report);
        }
    }
        
    private TestableVirusTotalClient _testableClient;
        
    [SetUp]
    public void SetupTestableClient()
    {
        _testableClient = new TestableVirusTotalClient(ValidApiKey);
    }
        
    [Test]
    public async Task VirusTotal_FileNameOver255Chars_ShouldReturnVirusScanResultWithErrorMessage()
    {
        // Arrange
        var fileBytes = "MZ"u8.ToArray();
        var fileName = new string('a', 256);
        
        // Act
        var result = await _testableClient.Scan(fileBytes, fileName);
        
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsClean, Is.False);
            Assert.That(result.Message, Contains.Substring("Filename cannot be longer than 255 characters. (Parameter 'filename')"));
            Assert.That(result.EnginesUsed, Is.Null);
        });
    }
        
    [Test]
    public void Constructor_WithValidApiKey_CreatesInstance()
    {
        // Act
        var client = new VirusTotalClient(ValidApiKey);
            
        // Assert
        Assert.That(client, Is.Not.Null);
    }
        
    [Test]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public void Constructor_WithNullOrEmptyApiKey_ThrowsArgumentException()
    {
        // Act & Assert
        var exNull = Assert.Throws<ArgumentException>(() => new VirusTotalClient(null!));
        var exEmpty = Assert.Throws<ArgumentException>(() => new VirusTotalClient(""));
            
        Assert.Multiple(() =>
        {
            Assert.That(exNull.Message, Does.Contain("cannot be null or empty"));
            Assert.That(exEmpty.Message, Does.Contain("cannot be null or empty"));
        });
    }
        
    [Test]
    public void Scan_WithNullBytes_ThrowsFileNotFoundException()
    {
        // Arrange
        var client = new VirusTotalClient(ValidApiKey);
            
        // Act & Assert
        var ex = Assert.ThrowsAsync<FileNotFoundException>(async () => 
            await client.Scan(null));
                
        Assert.That(ex.Message, Does.Contain("Empty bytes passed"));
    }

    // This test uses a partial mock approach to test the public API
    [Test]
    public async Task Scan_CallsScanBytesAsync_WithCorrectParameters()
    {
        // Arrange
        var mockClient = new Mock<VirusTotalClient>(ValidApiKey, false) { CallBase = true };
        var fileBytes = new byte[] { 0x01, 0x02, 0x03 };
        var expectedResult = new VirusScanResult(true, ["Engine1"], "Clean");
            
        // Setup the mock to return our expected result when ScanBytesAsync is called
        mockClient.Protected()
            .Setup<Task<VirusScanResult>>("ScanBytesAsync",
                ItExpr.Is<byte[]>(b => b == fileBytes),
                ItExpr.IsAny<string>())
            .ReturnsAsync(expectedResult);
            
        // Act
        var result = await mockClient.Object.Scan(fileBytes);
            
        // Assert
        Assert.That(result, Is.EqualTo(expectedResult));
            
        // Verify that ScanBytesAsync was called with the correct file bytes
        mockClient.Protected().Verify(
            "ScanBytesAsync", 
            Times.Once(), 
            ItExpr.Is<byte[]>(b => b == fileBytes),
            ItExpr.Is<string>(s => s.Contains("_scan.bin")));
    }
        
    [Test]
    public async Task Scan_ReturnsExpectedResult_WhenScanBytesAsyncReturnsCleanResult()
    {
        // Arrange
        var mockClient = new Mock<VirusTotalClient>(ValidApiKey, false) { CallBase = true };
        var fileBytes = new byte[] { 0x01, 0x02, 0x03 };
        var expectedResult = new VirusScanResult(
            true,
            ["Engine1", "Engine2"], 
            "Scan completed, file is clean");
            
        // Setup the mock to return a clean scan result
        mockClient.Protected()
            .Setup<Task<VirusScanResult>>("ScanBytesAsync", 
                ItExpr.IsAny<byte[]>(), 
                ItExpr.IsAny<string>())
            .ReturnsAsync(expectedResult);
            
        // Act
        var result = await mockClient.Object.Scan(fileBytes);
            
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(result.IsClean, Is.True);
            Assert.That(result.EnginesUsed, Has.Count.EqualTo(2));
            Assert.That(result.Message, Is.EqualTo("Scan completed, file is clean"));
        });
    }
        
    [Test]
    public async Task Scan_ReturnsExpectedResult_WhenScanBytesAsyncReturnsMaliciousResult()
    {
        // Arrange
        var mockClient = new Mock<VirusTotalClient>(ValidApiKey, false) { CallBase = true };
        var fileBytes = new byte[] { 0x01, 0x02, 0x03 };
        var expectedResult = new VirusScanResult(
            false,
            ["Engine1", "Engine2"], 
            "Scan completed, file contains threats");
            
        // Setup the mock to return a malicious scan result
        mockClient.Protected()
            .Setup<Task<VirusScanResult>>("ScanBytesAsync", 
                ItExpr.IsAny<byte[]>(), 
                ItExpr.IsAny<string>())
            .ReturnsAsync(expectedResult);
            
        // Act
        var result = await mockClient.Object.Scan(fileBytes);
            
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(result.IsClean, Is.False);
            Assert.That(result.Message, Is.EqualTo("Scan completed, file contains threats"));
        });
    }
        
    [Test]
    public void Scan_PropagatesExceptionsFromScanBytesAsync()
    {
        // Arrange
        var mockClient = new Mock<VirusTotalClient>(ValidApiKey, false) { CallBase = true };
        var fileBytes = new byte[] { 0x01, 0x02, 0x03 };
        var expectedException = new Exception("Test exception");
            
        // Setup the mock to throw an exception
        mockClient.Protected()
            .Setup<Task<VirusScanResult>>("ScanBytesAsync", 
                ItExpr.IsAny<byte[]>(), 
                ItExpr.IsAny<string>())
            .ThrowsAsync(expectedException);
            
        // Act & Assert
        var exception = Assert.ThrowsAsync<Exception>(async () => 
            await mockClient.Object.Scan(fileBytes));
                
        Assert.That(exception, Is.SameAs(expectedException));
    }

    // Tests for IsScanResultSafe method
    [Test]
    public void IsScanResultSafe_ReturnsTrue_WhenPositivesIsZero()
    {
        // Arrange
        var report = new FileReport
        {
            Positives = 0,
            Total = 10
        };
            
        // Act
        var result = TestableVirusTotalClient.PublicIsScanResultSafe(report);
            
        // Assert
        Assert.That(result, Is.True);
    }
        
    [Test]
    public void IsScanResultSafe_ReturnsFalse_WhenPositivesIsGreaterThanZero()
    {
        // Arrange - create reports with different positive counts
        var reportWithOnePositive = new FileReport
        {
            Positives = 1,
            Total = 10
        };
            
        var reportWithMultiplePositives = new FileReport
        {
            Positives = 5,
            Total = 10
        };
            
        // Act
        var resultWithOnePositive = TestableVirusTotalClient.PublicIsScanResultSafe(reportWithOnePositive);
        var resultWithMultiplePositives = TestableVirusTotalClient.PublicIsScanResultSafe(reportWithMultiplePositives);
            
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(resultWithOnePositive, Is.False, "Should return false when Positives is 1");
            Assert.That(resultWithMultiplePositives, Is.False, "Should return false when Positives is greater than 1");
        });
    }
        
    [Test]
    public void IsScanResultSafe_HandlesEdgeCases()
    {
        // Arrange - test with unusual values
        var reportWithNegativePositives = new FileReport
        {
            Positives = -1, // Technically invalid but testing boundary
            Total = 10
        };
            
        var reportWithZeroTotalButPositive = new FileReport
        {
            Positives = 1,
            Total = 0 // Unusual edge case
        };
            
        // Act
        var resultWithNegativePositives = TestableVirusTotalClient.PublicIsScanResultSafe(reportWithNegativePositives);
        var resultWithZeroTotalButPositive = TestableVirusTotalClient.PublicIsScanResultSafe(reportWithZeroTotalButPositive);
            
        // Assert
        Assert.Multiple(() =>
        {
            // Even with negative Positives (which is invalid), the method should just check if it's 0
            Assert.That(resultWithNegativePositives, Is.False, "Should return false when Positives is negative");
            Assert.That(resultWithZeroTotalButPositive, Is.False, "Should return false when Positives > 0, even if Total is 0");
        });
    }
}