using Application.Interfaces;
using Application.LogParse.Queries;
using Domain;
using Moq;

namespace Test.UnitTests.Application.LogParse;

[TestFixture]
public class GetUniqueIpAddressesQueryTests
{
    private Mock<ILogParser> _logParserMock;
    private Mock<IFileReader> _fileReaderMock;
    private Mock<IApplicationLifetime> _appLifetimeMock;
    private GetUniqueIpAddressesQueryHandler _handler;
    private CancellationToken _cancellationToken;

    [SetUp]
    public void Setup()
    {
        _logParserMock = new Mock<ILogParser>();
        _fileReaderMock = new Mock<IFileReader>();
        _appLifetimeMock = new Mock<IApplicationLifetime>();
        _handler = new GetUniqueIpAddressesQueryHandler(_logParserMock.Object, _fileReaderMock.Object, _appLifetimeMock.Object);
        _cancellationToken = CancellationToken.None;
    }

    [Test]
    public void GetUniqueIpAddressesQuery_CanBeCreated()
    {
        // Arrange & Act
        var query = new GetUniqueIpAddressesQuery();

        // Assert
        Assert.That(query, Is.Not.Null);
    }

    [Test]
    public async Task Handle_ReturnsUniqueIpAddresses_FromLogEntries()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();

        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 2, Key = "ip", Value = "192.168.1.2" },
            new() { LineNumber = 3, Key = "ip", Value = "192.168.1.1" }, // Duplicate
            new() { LineNumber = 4, Key = "ip", Value = "192.168.1.3" },
            new() { LineNumber = 5, Key = "url", Value = "/index.html" } // Non-IP entry
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetUniqueIpAddressesQuery();

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(3), "Should return exactly 3 unique IP addresses");
            Assert.That(result, Does.Contain("192.168.1.1"));
            Assert.That(result, Does.Contain("192.168.1.2"));
            Assert.That(result, Does.Contain("192.168.1.3"));
        });
    }

    [Test]
    public async Task Handle_ReturnsEmptyCollection_WhenNoIpEntriesExist()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();

        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "url", Value = "/index.html" },
            new() { LineNumber = 2, Key = "method", Value = "GET" }
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetUniqueIpAddressesQuery();

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.That(result, Is.Empty, "Should return empty collection when no IP entries exist");
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

        var query = new GetUniqueIpAddressesQuery();

        // Act & Assert
        Assert.ThrowsAsync<Exception>(async () =>
                await _handler.Handle(query, _cancellationToken),
            "Should throw exception when no log entries found");
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
            new() { LineNumber = 5, Key = "ip", Value = "192.168.1.3" }
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetUniqueIpAddressesQuery();

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(3), "Should only include IP entries");
            Assert.That(result.Any(r => r == "/page1.html"), Is.False, "Should not include URL values");
            Assert.That(result.Any(r => r == "GET"), Is.False, "Should not include method values");
        });
    }

    [Test]
    public async Task Handle_HandlesDuplicateIpAddresses_ReturningOnlyUnique()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();

        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 2, Key = "ip", Value = "192.168.1.2" },
            new() { LineNumber = 3, Key = "ip", Value = "192.168.1.1" }, // Duplicate
            new() { LineNumber = 4, Key = "ip", Value = "192.168.1.2" }, // Duplicate
            new() { LineNumber = 5, Key = "ip", Value = "192.168.1.1" }  // Duplicate
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetUniqueIpAddressesQuery();

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2), "Should return only 2 unique IP addresses");
            Assert.That(result, Does.Contain("192.168.1.1"));
            Assert.That(result, Does.Contain("192.168.1.2"));
        });
    }

    [Test]
    public async Task Handle_HandlesNullIpValues_IncludingThemInResult()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();

        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 2, Key = "ip", Value = null },
            new() { LineNumber = 3, Key = "ip", Value = "192.168.1.2" },
            new() { LineNumber = 4, Key = "ip", Value = null } // Duplicate null
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetUniqueIpAddressesQuery();

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(3), "Should return 3 unique IP values including null");
            Assert.That(result, Does.Contain("192.168.1.1"));
            Assert.That(result, Does.Contain("192.168.1.2"));
            Assert.That(result, Does.Contain(null));
        });
    }

    [Test]
    public async Task Handle_HandlesEmptyIpValues_IncludingThemInResult()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();

        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 2, Key = "ip", Value = "" },
            new() { LineNumber = 3, Key = "ip", Value = "192.168.1.2" },
            new() { LineNumber = 4, Key = "ip", Value = "" } // Duplicate empty
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetUniqueIpAddressesQuery();

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(3), "Should return 3 unique values including empty string");
            Assert.That(result, Does.Contain("192.168.1.1"));
            Assert.That(result, Does.Contain("192.168.1.2"));
            Assert.That(result, Does.Contain(""));
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
            .Returns([new ParsedLogLine { LineNumber = 1, Key = "ip", Value = "192.168.1.1" }]);

        var query = new GetUniqueIpAddressesQuery();

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

        var query = new GetUniqueIpAddressesQuery();

        // Act
        await _handler.Handle(query, _cancellationToken);

        // Assert
        _logParserMock.Verify(x => x.Parse(fileContents), Times.Once);
    }
        
    [Test]
    public async Task Handle_PreservesOrderOfFirstAppearance_ForIpAddresses()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();

        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "ip", Value = "192.168.1.3" },
            new() { LineNumber = 2, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 3, Key = "ip", Value = "192.168.1.2" },
            new() { LineNumber = 4, Key = "ip", Value = "192.168.1.3" }, // Duplicate
            new() { LineNumber = 5, Key = "ip", Value = "192.168.1.1" } // Duplicate
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetUniqueIpAddressesQuery();

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(3), "Should return 3 unique IP addresses");
                
            // Verify order of first appearance is preserved
            // Note: LINQ's Distinct() preserves the order of first appearance
            Assert.That(result[0], Is.EqualTo("192.168.1.3"), "First unique IP should be 192.168.1.3");
            Assert.That(result[1], Is.EqualTo("192.168.1.1"), "Second unique IP should be 192.168.1.1");
            Assert.That(result[2], Is.EqualTo("192.168.1.2"), "Third unique IP should be 192.168.1.2");
        });
    }
}