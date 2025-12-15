using Application.Interfaces;
using ConsoleApp;
using ConsoleApp.Interfaces;
using Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Test.UnitTests.ConsoleApp;

[TestFixture]
public class ProgramTests
{
    [Test]
    public void ServiceRegistration_RegistersAllRequiredServices()
    {
        // Arrange & Act
        var serviceCollection = new ServiceCollection()
            .AddInfrastructureServices()
            .AddConsoleAppServices()
            .AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
            });

        var serviceProvider = serviceCollection.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            // Assert
            Assert.That(serviceProvider.GetService<IMediator>(), Is.Not.Null, "Mediator should be registered");
            Assert.That(serviceProvider.GetService<IMenu>(), Is.Not.Null, "Menu should be registered");
            Assert.That(serviceProvider.GetService<IMenuItems>(), Is.Not.Null, "MenuItems should be registered");
            Assert.That(serviceProvider.GetService<IApplicationLifetime>(), Is.Not.Null, "ApplicationLifetime should be registered");
        });
    }

    [Test]
    public void MediatorIsNull_ThrowsException()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => {
            var mediator = serviceProvider.GetService<IMediator>();
            if (mediator == null)
                throw new Exception("Unable to resolve mediator");
        });

        Assert.That(exception.Message, Is.EqualTo("Unable to resolve mediator"));
    }
        
    [Test]
    public async Task Menu_IsInvokedCorrectly()
    {
        // Arrange
        var menuMock = new Mock<IMenu>();
        menuMock.Setup(m => m.PromptUserForSelection())
            .Returns(Task.CompletedTask)
            .Verifiable();

        var mediatorMock = new Mock<IMediator>();
            
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(mediatorMock.Object);
        serviceCollection.AddSingleton(menuMock.Object);
            
        var serviceProvider = serviceCollection.BuildServiceProvider();

        // Act
        var mediator = serviceProvider.GetService<IMediator>();
        if (mediator == null)
            throw new Exception("Unable to resolve mediator");

        var menu = serviceProvider.GetService<IMenu>();
        await menu!.PromptUserForSelection();

        // Assert
        menuMock.Verify(m => m.PromptUserForSelection(), Times.Once);
    }

    [Test]
    public void ServiceRegistration_InfrastructureServices_AreRegistered()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
            
        // Act
        serviceCollection.AddInfrastructureServices();
            
        // Assert - This test assumes AddInfrastructureServices registers at least some services
        Assert.That(serviceCollection, Is.Not.Empty, 
            "AddInfrastructureServices should register at least one service");
    }

    [Test]
    public void ServiceRegistration_ConsoleAppServices_AreRegistered()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
            
        // Act
        serviceCollection.AddConsoleAppServices();
            
        // Assert
        var menuRegistration = serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(IMenu));
        var menuItemsRegistration = serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(IMenuItems));
        var appLifetimeRegistration = serviceCollection.FirstOrDefault(sd => sd.ServiceType == typeof(IApplicationLifetime));

        Assert.Multiple(() =>
        {
            Assert.That(menuRegistration, Is.Not.Null, "IMenu should be registered");
            Assert.That(menuItemsRegistration, Is.Not.Null, "IMenuItems should be registered");
            Assert.That(appLifetimeRegistration, Is.Not.Null, "IApplicationLifetime should be registered");
        });
    }

    [Test]
    public void ServiceRegistration_MediatR_IsRegistered()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
            
        // Act
        serviceCollection.AddMediatR(configuration => 
        {
            configuration.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
            
        // Assert
        Assert.That(serviceProvider.GetService<IMediator>(), Is.Not.Null, "Mediator should be registered");
    }
}