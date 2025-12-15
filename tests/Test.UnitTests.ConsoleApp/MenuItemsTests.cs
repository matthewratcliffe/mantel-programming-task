using Application.Interfaces;
using Application.LogParse.Queries;
using ConsoleApp.Interfaces.Concrete;
using Application.LogParse.Models;
using MediatR;
using Moq;

namespace Test.UnitTests.ConsoleApp;

[TestFixture]
public class MenuItemsTests
{
    private Mock<IMediator> _mediatorMock;
    private Mock<IApplicationLifetime> _appLifetimeMock;
    private MenuItems _menuItems;
    private StringWriter _consoleOutput;
    private TextWriter _originalOutput;

    [SetUp]
    public void Setup()
    {
        // Set up mocks
        _mediatorMock = new Mock<IMediator>();
            
        _appLifetimeMock = new Mock<IApplicationLifetime>();
        _appLifetimeMock.Setup(m => m.Exit()).Verifiable();
            
        // Create instance to test
        _menuItems = new MenuItems(_mediatorMock.Object, _appLifetimeMock.Object);

        // Set up console redirection to capture output
        _originalOutput = Console.Out;
        _consoleOutput = new StringWriter();
        Console.SetOut(_consoleOutput);
    }

    [TearDown]
    public void Teardown()
    {
        // Restore original console
        Console.SetOut(_originalOutput);
        _consoleOutput.Dispose();
    }

    [Test]
    public async Task HandleUniqueIpAddresses_DisplaysCorrectMessage()
    {
        // Arrange
        var ipAddresses = new List<string> { "192.168.1.1", "10.0.0.1", "172.16.0.1" };
        _mediatorMock.Setup(m => m.Send(
                It.IsAny<GetUniqueIpAddressesQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ipAddresses);

        // Act
        await _menuItems.HandleUniqueIpAddresses();
        var output = _consoleOutput.ToString().Trim();

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.IsAny<GetUniqueIpAddressesQuery>(),
            It.IsAny<CancellationToken>()), Times.Once);
            
        Assert.That(output, Is.EqualTo("There are 3 unique IP addresses in the log file"));
    }

    [Test]
    public async Task HandleTopXMostActiveIps_DisplaysCorrectIPsWithDefaultCount()
    {
        // Arrange
        var topIps = new List<RankGroup>
        {
            new() { Rank = 1, HitCount = 10, Items = ["192.168.1.1"] },
            new() { Rank = 2, HitCount = 8, Items = ["10.0.0.1"] },
            new() { Rank = 3, HitCount = 5, Items = ["172.16.0.1"] }
        };
            
        _mediatorMock.Setup(m => m.Send(
                It.Is<GetTopMostActiveIpAddressesQuery>(q => q.Count == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(topIps);

        // Act
        await _menuItems.HandleTopXMostActiveIps();
        var output = _consoleOutput.ToString();

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetTopMostActiveIpAddressesQuery>(q => q.Count == 3),
            It.IsAny<CancellationToken>()), Times.Once);
            
        Assert.That(output, Does.Contain("Most active IP addresses:"));
        Assert.That(output, Does.Contain("1st (10 hits): 192.168.1.1"));
        Assert.That(output, Does.Contain("2nd (8 hits): 10.0.0.1"));
        Assert.That(output, Does.Contain("3rd (5 hits): 172.16.0.1"));
    }

    [Test]
    public async Task HandleTopXMostActiveIps_UsesSpecifiedCount()
    {
        // Arrange
        var topIps = new List<RankGroup>
        {
            new() { Rank = 1, HitCount = 10, Items = ["192.168.1.1"] },
            new() { Rank = 2, HitCount = 8, Items = ["10.0.0.1"] }
        };
            
        _mediatorMock.Setup(m => m.Send(
                It.Is<GetTopMostActiveIpAddressesQuery>(q => q.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(topIps);

        // Act
        await _menuItems.HandleTopXMostActiveIps(2);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetTopMostActiveIpAddressesQuery>(q => q.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleTopXVisitedUrls_DisplaysCorrectUrlsWithDefaultCount()
    {
        // Arrange
        var topUrls = new List<RankGroup>
        {
            new() { Rank = 1, HitCount = 15, Items = ["/index.html"] },
            new() { Rank = 2, HitCount = 10, Items = ["/about"] },
            new() { Rank = 3, HitCount = 5, Items = ["/contact"] }
        };
            
        _mediatorMock.Setup(m => m.Send(
                It.Is<GetTopVisitedUrlsQuery>(q => q.Count == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(topUrls);

        // Act
        await _menuItems.HandleTopXVisitedUrls();
        var output = _consoleOutput.ToString();

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetTopVisitedUrlsQuery>(q => q.Count == 3),
            It.IsAny<CancellationToken>()), Times.Once);
            
        Assert.That(output, Does.Contain("Most visited URLs:"));
        Assert.That(output, Does.Contain("1st (15 hits): /index.html"));
        Assert.That(output, Does.Contain("2nd (10 hits): /about"));
        Assert.That(output, Does.Contain("3rd (5 hits): /contact"));
    }

    [Test]
    public async Task HandleTopXVisitedUrls_UsesSpecifiedCount()
    {
        // Arrange
        var topUrls = new List<RankGroup>
        {
            new() { Rank = 1, HitCount = 15, Items = ["/index.html"] },
            new() { Rank = 2, HitCount = 10, Items = ["/about"] },
            new() { Rank = 3, HitCount = 5, Items = ["/contact"] },
            new() { Rank = 4, HitCount = 3, Items = ["/products"] }
        };
            
        _mediatorMock.Setup(m => m.Send(
                It.Is<GetTopVisitedUrlsQuery>(q => q.Count == 4),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(topUrls);

        // Act
        await _menuItems.HandleTopXVisitedUrls(4);

        // Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetTopVisitedUrlsQuery>(q => q.Count == 4),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void HandleExit_DisplaysExitMessageAndCallsEnvironmentExit()
    {
        // Arrange
        _menuItems.HandleExit();
            
        // Assert - Check the console output
        var output = _consoleOutput.ToString().Trim();
        Assert.That(output, Is.EqualTo("Exiting program..."));
            
        _appLifetimeMock.Verify(m => m.Exit(), Times.Once);
    }

    [Test]
    public async Task HandleUniqueIpAddresses_HandlesEmptyResult()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(
                It.IsAny<GetUniqueIpAddressesQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        await _menuItems.HandleUniqueIpAddresses();
        var output = _consoleOutput.ToString().Trim();

        // Assert
        Assert.That(output, Is.EqualTo("There are 0 unique IP addresses in the log file"));
    }

    [Test]
    public async Task HandleTopXMostActiveIps_HandlesEmptyResult()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(
                It.IsAny<GetTopMostActiveIpAddressesQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RankGroup>());

        // Act
        await _menuItems.HandleTopXMostActiveIps();
        var output = _consoleOutput.ToString();

        // Assert
        Assert.That(output, Does.Contain("Most active IP addresses:"));
        // No IP addresses should be listed
        Assert.That(output.Split('\n'), Has.Length.EqualTo(2)); // Just the header line and a newline
    }

    [Test]
    public async Task HandleTopXVisitedUrls_HandlesEmptyResult()
    {
        // Arrange
        _mediatorMock.Setup(m => m.Send(
                It.IsAny<GetTopVisitedUrlsQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RankGroup>());

        // Act
        await _menuItems.HandleTopXVisitedUrls();
        var output = _consoleOutput.ToString();

        // Assert
        Assert.That(output, Does.Contain("Most visited URLs:"));
        // No URLs should be listed
        Assert.That(output.Split('\n'), Has.Length.EqualTo(2)); // Just the header line and a newline
    }
}