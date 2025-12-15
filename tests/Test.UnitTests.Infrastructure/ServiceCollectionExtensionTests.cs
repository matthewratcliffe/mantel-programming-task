using Application.Interfaces;
using Infrastructure;
using Infrastructure.Services.FileReader;
using Infrastructure.Services.LogParser;
using Microsoft.Extensions.DependencyInjection;

namespace Test.UnitTests.Infrastructure;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    private IServiceCollection _services;
    private ServiceProvider? _serviceProvider;
        
    [SetUp]
    public void Setup()
    {
        _services = new ServiceCollection();
    }
        
    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }
        
    [Test]
    public void AddInfrastructureServices_RegistersAllRequiredServices()
    {
        // Arrange & Act
        _services.AddInfrastructureServices();
        _serviceProvider = _services.BuildServiceProvider();
            
        // Assert
        var logParser = _serviceProvider.GetService<ILogParser>();
        var fileReader = _serviceProvider.GetService<IFileReader>();
        var virusScan = _serviceProvider.GetService<IVirusScanServiceFactory>();
        
        Assert.Multiple(() =>
        {
            Assert.That(logParser, Is.Not.Null);
            Assert.That(fileReader, Is.Not.Null);
            Assert.That(virusScan, Is.Not.Null);
            Assert.That(logParser, Is.TypeOf<RegexParserService>());
            Assert.That(fileReader, Is.TypeOf<FileReader>());
        });
    }
        
    [Test]
    public void AddInfrastructureServices_RegistersVirusFactoryService()
    {
        // Arrange
        var originalValue = Environment.GetEnvironmentVariable("VIRUSTOTAL_API_KEY");
        try
        {
            // Set environment variable
            Environment.SetEnvironmentVariable("VIRUSTOTAL_API_KEY", "test-api-key");
                
            // Act
            _services.AddInfrastructureServices();
                
            // Note: We're not building service provider here because VirusTotalService
            // constructor will try to use the API key. Instead, we'll check the service
            // descriptor directly.
                
            // Assert
            var hasVirusTotalService = HasServiceRegistration(_services, typeof(IVirusScanServiceFactory), null!);
            Assert.That(hasVirusTotalService, Is.True, "VirusScanServiceFactory should be registered");
        }
        finally
        {
            // Restore original environment variable
            Environment.SetEnvironmentVariable("VIRUSTOTAL_API_KEY", originalValue);
        }
    }
        
    [Test]
    public void AddInfrastructureServices_RegistersServicesWithCorrectLifetime()
    {
        // Arrange & Act
        _services.AddInfrastructureServices();
            
        // Assert - verify all services are registered with Scoped lifetime
        var logParserHasScopedLifetime = HasScopedLifetime(_services, typeof(ILogParser));
        var fileReaderHasScopedLifetime = HasScopedLifetime(_services, typeof(IFileReader));
        var virusScanHasSingletonLifetime = HasSingletonLifetime(_services, typeof(IVirusScanServiceFactory));
        Assert.Multiple(() =>
        {
            Assert.That(logParserHasScopedLifetime, Is.True, "ILogParser should have Scoped lifetime");
            Assert.That(fileReaderHasScopedLifetime, Is.True, "IFileReader should have Scoped lifetime");
            Assert.That(virusScanHasSingletonLifetime, Is.True, "IVirusScanServiceFactory should have Singleton lifetime");
        });
    }
        
    [Test]
    public void AddInfrastructureServices_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var initialServices = _services;
            
        // Act
        var result = _services.AddInfrastructureServices();
            
        // Assert
        Assert.That(result, Is.SameAs(initialServices), "Method should return the same ServiceCollection instance for chaining");
    }
        
    [Test]
    public void AddInfrastructureServices_CreatesNewInstances_OnEachServiceRequest()
    {
        // Arrange
        Environment.SetEnvironmentVariable("VIRUSTOTAL_API_KEY", null);
        _services.AddInfrastructureServices();
        _serviceProvider = _services.BuildServiceProvider();
            
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();
            
        // Act - Get services from different scopes
        var logParser1 = scope1.ServiceProvider.GetRequiredService<ILogParser>();
        var logParser2 = scope2.ServiceProvider.GetRequiredService<ILogParser>();
            
        var fileReader1 = scope1.ServiceProvider.GetRequiredService<IFileReader>();
        var fileReader2 = scope2.ServiceProvider.GetRequiredService<IFileReader>();
            
        var virusScan1 = scope1.ServiceProvider.GetRequiredService<IVirusScanServiceFactory>();
        var virusScan2 = scope2.ServiceProvider.GetRequiredService<IVirusScanServiceFactory>();
        
        // Assert
        Assert.Multiple(() =>
        {
            // Services should be different instances across scopes
            Assert.That(logParser1, Is.Not.SameAs(logParser2), "ILogParser should be scoped and return different instances");
            Assert.That(fileReader1, Is.Not.SameAs(fileReader2), "IFileReader should be scoped and return different instances");
        });

        // But same instance within a scope
        var logParserAgain1 = scope1.ServiceProvider.GetRequiredService<ILogParser>();
        Assert.Multiple(() =>
        {
            Assert.That(virusScan1, Is.SameAs(virusScan2), "IVirusScan should be scoped and return different instances");
            Assert.That(logParser1, Is.SameAs(logParserAgain1), "ILogParser should return same instance within same scope");
        });
    }
        
    #region Helper Methods
        
    private static bool HasServiceRegistration(IServiceCollection services, Type serviceType, Type implementationType)
    {
        return services.Any(serviceDescriptor => serviceDescriptor.ServiceType == serviceType
                                                 && serviceDescriptor.ImplementationType == implementationType);
    }
        
    private static bool HasScopedLifetime(IServiceCollection services, Type serviceType)
    {
        return services.Any(
            serviceDescriptor => serviceDescriptor.ServiceType == serviceType
                                 && serviceDescriptor.Lifetime == ServiceLifetime.Scoped);
    }
        
    private static bool HasSingletonLifetime(IServiceCollection services, Type serviceType)
    {
        return services.Any(
            serviceDescriptor => serviceDescriptor.ServiceType == serviceType
                                 && serviceDescriptor.Lifetime == ServiceLifetime.Singleton);
    }
        
    #endregion
}