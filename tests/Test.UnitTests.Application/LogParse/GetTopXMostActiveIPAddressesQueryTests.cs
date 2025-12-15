using Application.Interfaces;
using Application.LogParse.Queries;
using Domain;
using Moq;

namespace Test.UnitTests.Application.LogParse;

[TestFixture]
public class GetTopMostActiveIpAddressesQueryTests
{
    private Mock<ILogParser> _logParserMock;
    private Mock<IFileReader> _fileReaderMock;
    private Mock<IApplicationLifetime> _appLifetimeMock;
    private GetTopMostActiveIpAddressesQueryHandler _handler;
    private CancellationToken _cancellationToken;

    [SetUp]
    public void Setup()
    {
        _logParserMock = new Mock<ILogParser>();
        _fileReaderMock = new Mock<IFileReader>();
        _appLifetimeMock = new Mock<IApplicationLifetime>();
        _handler = new GetTopMostActiveIpAddressesQueryHandler(_logParserMock.Object, _fileReaderMock.Object, _appLifetimeMock.Object);
        _cancellationToken = CancellationToken.None;
    }

    [Test]
    public void GetTopMostActiveIpAddressesQuery_DefaultCount_ShouldBeThree()
    {
        // Arrange & Act
        var query = new GetTopMostActiveIpAddressesQuery();

        // Assert
        Assert.That(query.Count, Is.EqualTo(3));
    }

    [Test]
    public void GetTopMostActiveIpAddressesQuery_CustomCount_SetsCorrectly()
    {
        // Arrange & Act
        var query = new GetTopMostActiveIpAddressesQuery { Count = 5 };

        // Assert
        Assert.That(query.Count, Is.EqualTo(5));
    }

    [Test]
    public async Task Handle_ReturnsTopPlaces_WithCounts_AndItems()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();
            
        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 2, Key = "ip", Value = "192.168.1.2" },
            new() { LineNumber = 3, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 4, Key = "ip", Value = "192.168.1.3" },
            new() { LineNumber = 5, Key = "ip", Value = "192.168.1.2" },
            new() { LineNumber = 6, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 7, Key = "url", Value = "/index.html" } // Non-IP entry
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetTopMostActiveIpAddressesQuery { Count = 2 };

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2), "Should return exactly 2 places (ranks)");
            Assert.That(result[0].Rank, Is.EqualTo(1));
            Assert.That(result[0].HitCount, Is.EqualTo(3));
            Assert.That(result[0].Items, Is.EquivalentTo(new[] { "192.168.1.1" }));
            Assert.That(result[1].Rank, Is.EqualTo(2));
            Assert.That(result[1].HitCount, Is.EqualTo(2));
            Assert.That(result[1].Items, Is.EquivalentTo(new[] { "192.168.1.2" }));
        });
    }

    [Test]
    public async Task Handle_TakesOnlyRequestedPlaces_WhenMoreAvailable()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();
            
        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 2, Key = "ip", Value = "192.168.1.2" },
            new() { LineNumber = 3, Key = "ip", Value = "192.168.1.3" },
            new() { LineNumber = 4, Key = "ip", Value = "192.168.1.4" },
            new() { LineNumber = 5, Key = "ip", Value = "192.168.1.5" }
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetTopMostActiveIpAddressesQuery { Count = 3 };

        // Act
        var result = await _handler.Handle(query, _cancellationToken);

        // Assert
        // All have equal count (1), so only 1 place should be returned
        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_FiltersOnlyIpEntries_IgnoringOtherKeys()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();
            
        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 2, Key = "url", Value = "/page1.html" },
            new() { LineNumber = 3, Key = "ip", Value = "192.168.1.2" },
            new() { LineNumber = 4, Key = "method", Value = "GET" },
            new() { LineNumber = 5, Key = "ip", Value = "192.168.1.1" }
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetTopMostActiveIpAddressesQuery();

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.GreaterThanOrEqualTo(1));
            foreach (var group in result)
            {
                // Ensure only IP addresses are included in items
                Assert.That(group.Items.All(i => i!.StartsWith("192.168.1.") ));
            }
        });
    }

    [Test]
    public void Handle_ThrowsException_WhenNoLogEntriesFound()
    {
        // Arrange
        var fileContents = "empty log file"u8.ToArray();
            
        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns([]);

        var query = new GetTopMostActiveIpAddressesQuery();

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () => 
                await _handler.Handle(query, _cancellationToken), 
            "Should throw exception when no log entries found");
    }

    [Test]
    public async Task Handle_ReturnsAllPlaces_WhenRequestedCountIsHigher()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();
            
        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 2, Key = "ip", Value = "192.168.1.2" },
            new() { LineNumber = 3, Key = "ip", Value = "192.168.1.3" }
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetTopMostActiveIpAddressesQuery { Count = 10 };

        // Act
        var result = await _handler.Handle(query, _cancellationToken);

        // Assert
        // All have equal counts (1), so only one place should be returned
        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_HandlesIpsWithEqualCounts_ReturnsAllTiedInSamePlace()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();
            
        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 2, Key = "ip", Value = "192.168.1.2" },
            new() { LineNumber = 3, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 4, Key = "ip", Value = "192.168.1.2" },
            new() { LineNumber = 5, Key = "ip", Value = "192.168.1.3" }
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetTopMostActiveIpAddressesQuery { Count = 2 };

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2), "Should return 2 places (2nd is the 3rd IP)");
            Assert.That(result[0].Rank, Is.EqualTo(1));
            Assert.That(result[0].HitCount, Is.EqualTo(2));
            Assert.That(result[0].Items, Is.EquivalentTo(new[] { "192.168.1.1", "192.168.1.2" }));
        });
    }

    [Test]
    public async Task Handle_CallsFileReader_ToGetLogContents()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();
            
        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents)
            .Verifiable();

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns([new ParsedLogLine(lineNumber: 1, key: "ip", value: "192.168.1.1")]);

        var query = new GetTopMostActiveIpAddressesQuery();

        // Act
        await _handler.Handle(query, _cancellationToken);

        // Assert
        _fileReaderMock.Verify(x => x.ReadAllBytes(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task Handle_CallsLogParser_WithCorrectFileContents()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();
            
        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        _logParserMock.Setup(x => x.Parse(It.Is<byte[]>(b => b == fileContents)))
            .Returns([new ParsedLogLine { LineNumber = 1, Key = "ip", Value = "192.168.1.1" }])
            .Verifiable();

        var query = new GetTopMostActiveIpAddressesQuery();

        // Act
        await _handler.Handle(query, _cancellationToken);

        // Assert
        _logParserMock.Verify(x => x.Parse(fileContents), Times.Once);
    }
}