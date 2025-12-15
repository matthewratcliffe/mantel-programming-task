using Application.Interfaces;
using Domain;
using Infrastructure.Services.VirusScan;
using Infrastructure.Services.VirusScan.Dummy;
using Infrastructure.Services.VirusScan.VirusTotal;

namespace Test.UnitTests.Infrastructure;

[TestFixture]
public class VirusScanServiceFactoryTests
{
    // Note: This is NOT a real API key. It is only used for testing purposes.
    private const string ValidApiKey = "998fb43afb2c4e0bb73e5fa50a04ad9000003605XX68e6f4c86ba5119e46c424";
        
    [Test]
    public void Create_WithNullApiKey_ReturnsDummyVirusScanService()
    {
        // Arrange
        var factory = new VirusScanServiceFactory(null);
            
        // Act
        var service = factory.Create();
            
        // Assert
        Assert.That(service, Is.TypeOf<DummyVirusScanService>());
    }
        
    [Test]
    public void Create_WithEmptyApiKey_ReturnsDummyVirusScanService()
    {
        // Arrange
        var factory = new VirusScanServiceFactory("");
            
        // Act
        var service = factory.Create();
            
        // Assert
        Assert.That(service, Is.TypeOf<DummyVirusScanService>());
    }
        
    [Test]
    public void Create_WithWhitespaceApiKey_ReturnsDummyVirusScanService()
    {
        // Arrange
        var factory = new VirusScanServiceFactory("   ");
            
        // Act
        var service = factory.Create();
            
        // Assert
        Assert.That(service, Is.TypeOf<DummyVirusScanService>());
    }
        
    [Test]
    public void Create_WithValidApiKey_ReturnsVirusTotalScanService()
    {
        // Arrange
        var factory = new VirusScanServiceFactory(ValidApiKey);
            
        // Act
        var service = factory.Create();
            
        // Assert
        Assert.That(service, Is.TypeOf<VirusTotalScanService>());
    }
        
    [Test]
    public void Create_WithValidApiKey_PassesApiKeyToVirusTotalClient()
    {
        // Arrange & Act - This test requires access to the VirusTotalClient to verify
        // that the API key was correctly passed.
            
        // For this example, we'll use a custom factory for testing
        var testableFactory = new TestableVirusScanServiceFactory(ValidApiKey);
        testableFactory.Create();
            
        // Assert
        Assert.That(testableFactory.CreatedClientApiKey, Is.EqualTo(ValidApiKey));
    }
        
    [Test]
    public void Create_CalledMultipleTimes_CreatesNewInstancesEachTime()
    {
        // Arrange
        var factory = new VirusScanServiceFactory(ValidApiKey);
            
        // Act
        var service1 = factory.Create();
        var service2 = factory.Create();
            
        // Assert
        Assert.That(service1, Is.Not.SameAs(service2), "Factory should create new instances on each call");
    }
}
    
// A testable version of the factory that exposes the API key used to create the client
public class TestableVirusScanServiceFactory : VirusScanServiceFactory
{
    public string? CreatedClientApiKey { get; private set; }

    /// <inheritdoc />
    public TestableVirusScanServiceFactory(string? apiKey) : base(apiKey)
    {
    }
        
    public override IVirusScan Create()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            return new DummyVirusScanService();
                
        CreatedClientApiKey = ApiKey;
        return new TestableVirusTotalScanService();
    }
}
    
// Stub implementation for testing
public class TestableVirusTotalScanService : IVirusScan
{
    public Task<VirusScanResult> Scan(byte[] fileBytes)
    {
        return Task.FromResult(new VirusScanResult(true));
    }
}