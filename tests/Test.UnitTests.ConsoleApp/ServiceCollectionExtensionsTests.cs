using ConsoleApp;
using ConsoleApp.Interfaces;
using Application.Interfaces;
using ConsoleApp.Interfaces.Concrete;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Test.UnitTests.ConsoleApp;

public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddConsoleAppServices_RegistersServicesWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
            
        // Act
        services.AddConsoleAppServices();
            
        // Assert
        var menuDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMenu));
        var menuItemsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMenuItems));
            
        Assert.Multiple(() =>
        {
            Assert.That(menuDescriptor, Is.Not.Null);
            Assert.That(menuItemsDescriptor, Is.Not.Null);
            Assert.That(menuDescriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
            Assert.That(menuItemsDescriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
        });
    }

    [Test]
    public void AddConsoleAppServices_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
            
        // Act
        var result = services.AddConsoleAppServices();
            
        // Assert
        Assert.That(result, Is.SameAs(services));
    }

    [Test]
    public void AddConsoleAppServices_RegistersExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();
            
        // Add mock dependencies needed by MenuItems
        var mediatorMock = new Mock<IMediator>();
        var fileReaderMock = new Mock<IFileReader>();
            
        services.AddSingleton(mediatorMock.Object);
        services.AddSingleton(fileReaderMock.Object);
            
        // Act
        services.AddConsoleAppServices();
        var serviceProvider = services.BuildServiceProvider();
            
        // Assert
        var menu = serviceProvider.GetService<IMenu>();
        var menuItems = serviceProvider.GetService<IMenuItems>();
            
        Assert.Multiple(() =>
        {
            Assert.That(menu, Is.Not.Null);
            Assert.That(menuItems, Is.Not.Null);
            Assert.That(menu, Is.TypeOf<Menu>());
            Assert.That(menuItems, Is.TypeOf<MenuItems>());
        });
    }
        
    [Test]
    public void AddConsoleAppServices_CanResolveAllRegisteredServices()
    {
        // Arrange
        var services = new ServiceCollection();
            
        // Add mock dependencies needed by MenuItems and Menu
        var mediatorMock = new Mock<IMediator>();
        var fileReaderMock = new Mock<IFileReader>();
            
        services.AddSingleton(mediatorMock.Object);
        services.AddSingleton(fileReaderMock.Object);
            
        // Act
        services.AddConsoleAppServices();
        var serviceProvider = services.BuildServiceProvider();
            
        // Assert - This will throw if services can't be resolved
        Assert.DoesNotThrow(() => {
            _ = serviceProvider.GetRequiredService<IMenu>();
            _ = serviceProvider.GetRequiredService<IMenuItems>();
        });
    }
}