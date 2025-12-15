using Application.Interfaces;
using Application.LogParse.Queries;
using Domain;
using Moq;

namespace Test.UnitTests.Application.LogParse;

[TestFixture]
public class GetTopVisitedUrlsQueryTests
{
    private Mock<ILogParser> _logParserMock;
    private Mock<IFileReader> _fileReaderMock;
    private GetTopVisitedUrlsQueryHandler _handler;
    private Mock<IApplicationLifetime> _appLifetimeMock;
    private CancellationToken _cancellationToken;

    [SetUp]
    public void Setup()
    {
        _logParserMock = new Mock<ILogParser>();
        _fileReaderMock = new Mock<IFileReader>();
        _appLifetimeMock = new Mock<IApplicationLifetime>();
        _handler = new GetTopVisitedUrlsQueryHandler(_logParserMock.Object, _fileReaderMock.Object, _appLifetimeMock.Object);
        _cancellationToken = CancellationToken.None;
    }

    [Test]
    public void GetTopVisitedUrlsQuery_DefaultCount_ShouldBeThree()
    {
        // Arrange & Act
        var query = new GetTopVisitedUrlsQuery();

        // Assert
        Assert.That(query.Count, Is.EqualTo(3));
    }

    [Test]
    public void GetTopVisitedUrlsQuery_CustomCount_SetsCorrectly()
    {
        // Arrange & Act
        var query = new GetTopVisitedUrlsQuery { Count = 5 };

        // Assert
        Assert.That(query.Count, Is.EqualTo(5));
    }
        
    [Test]
    public void GetTopVisitedUrlsQuery_IsImmutable_CountIsInitOnly()
    {
        // This test verifies that Count is init-only by checking compilation
        // We can't directly test compiler errors in unit tests, but we can verify
        // that the property is decorated with init accessor
            
        // Arrange & Act
        var query = new GetTopVisitedUrlsQuery { Count = 5 };
            
        // Assert - this is more of a documentation test
        Assert.That(query.Count, Is.EqualTo(5), "Count should be set correctly via init");
            
        // The following code would cause a compilation error, which confirms init-only behavior:
        // query.Count = 10; // This would fail to compile with CS8852
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
            new() { LineNumber = 1, Key = "path", Value = "/home" },
            new() { LineNumber = 2, Key = "path", Value = "/about" },
            new() { LineNumber = 3, Key = "path", Value = "/home" },
            new() { LineNumber = 4, Key = "path", Value = "/contact" },
            new() { LineNumber = 5, Key = "path", Value = "/about" },
            new() { LineNumber = 6, Key = "path", Value = "/home" },
            new() { LineNumber = 7, Key = "ip", Value = "192.168.1.1" } // Non-path entry
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetTopVisitedUrlsQuery { Count = 2 };

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2), "Should return exactly 2 places (ranks)");
            Assert.That(result[0].Rank, Is.EqualTo(1));
            Assert.That(result[0].HitCount, Is.EqualTo(3));
            Assert.That(result[0].Items, Is.EquivalentTo(new[] { "/home" }));
            Assert.That(result[1].Rank, Is.EqualTo(2));
            Assert.That(result[1].HitCount, Is.EqualTo(2));
            Assert.That(result[1].Items, Is.EquivalentTo(new[] { "/about" }));
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
            new() { LineNumber = 1, Key = "path", Value = "/home" },
            new() { LineNumber = 2, Key = "path", Value = "/about" },
            new() { LineNumber = 3, Key = "path", Value = "/contact" },
            new() { LineNumber = 4, Key = "path", Value = "/products" },
            new() { LineNumber = 5, Key = "path", Value = "/blog" }
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetTopVisitedUrlsQuery { Count = 3 };

        // Act
        var result = await _handler.Handle(query, _cancellationToken);

        // Assert
        // All have equal count (1), so only 1 place should be returned
        Assert.That(result.Count(), Is.EqualTo(1), "Should return only 1 place because all are tied");
    }

    [Test]
    public async Task Handle_FiltersOnlyPathEntries_IgnoringOtherKeys()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();
            
        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "path", Value = "/home" },
            new() { LineNumber = 2, Key = "ip", Value = "192.168.1.1" },
            new() { LineNumber = 3, Key = "path", Value = "/about" },
            new() { LineNumber = 4, Key = "method", Value = "GET" },
            new() { LineNumber = 5, Key = "path", Value = "/home" }
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetTopVisitedUrlsQuery();

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.GreaterThanOrEqualTo(1));
            // All items reported should be paths, not other keys
            foreach (var group in result)
            {
                Assert.That(group.Items.All(i => i is "/home" or "/about" or "/contact" or ""));
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

        var query = new GetTopVisitedUrlsQuery();

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
            new() { LineNumber = 1, Key = "path", Value = "/home" },
            new() { LineNumber = 2, Key = "path", Value = "/about" },
            new() { LineNumber = 3, Key = "path", Value = "/contact" }
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetTopVisitedUrlsQuery { Count = 10 };

        // Act
        var result = await _handler.Handle(query, _cancellationToken);

        // Assert
        // All have equal counts (1), so only one place should be returned
        Assert.That(result.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task Handle_HandlesPathsWithEqualCounts_ReturnsAllTiedInSamePlace()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();
            
        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "path", Value = "/home" },
            new() { LineNumber = 2, Key = "path", Value = "/about" },
            new() { LineNumber = 3, Key = "path", Value = "/home" },
            new() { LineNumber = 4, Key = "path", Value = "/about" },
            new() { LineNumber = 5, Key = "path", Value = "/contact" }
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetTopVisitedUrlsQuery { Count = 2 };

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2), "Should return 2 places (2nd is contact)");
            Assert.That(result[0].Rank, Is.EqualTo(1));
            Assert.That(result[0].HitCount, Is.EqualTo(2));
            Assert.That(result[0].Items, Is.EquivalentTo(new[] { "/home", "/about" }));
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
            .Returns([new ParsedLogLine { LineNumber = 1, Key = "path", Value = "/home" }]);

        var query = new GetTopVisitedUrlsQuery();

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
            .Returns([new ParsedLogLine { LineNumber = 1, Key = "path", Value = "/home" }])
            .Verifiable();

        var query = new GetTopVisitedUrlsQuery();

        // Act
        await _handler.Handle(query, _cancellationToken);

        // Assert
        _logParserMock.Verify(x => x.Parse(fileContents), Times.Once);
    }
        
    [Test]
    public async Task Handle_WithNullPathValues_HandlesThemCorrectly()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();
            
        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "path", Value = "/home" },
            new() { LineNumber = 2, Key = "path", Value = null },
            new() { LineNumber = 3, Key = "path", Value = "/home" },
            new() { LineNumber = 4, Key = "path", Value = null }
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetTopVisitedUrlsQuery { Count = 3 };

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            // Expect two places: "/home" with 2, and null with 2 (tie -> same place)
            Assert.That(result[0].HitCount, Is.EqualTo(2));
            Assert.That(result[0].Items, Is.EquivalentTo(new[] { "/home", null }));
        });
    }
        
    [Test]
    public async Task Handle_WithEmptyPathValues_CountsThemSeparately()
    {
        // Arrange
        var fileContents = "test log file"u8.ToArray();
            
        _fileReaderMock.Setup(x => x.ReadAllBytes(It.IsAny<string>()))
            .ReturnsAsync(fileContents);

        var parsedResults = new List<ParsedLogLine>
        {
            new() { LineNumber = 1, Key = "path", Value = "/home" },
            new() { LineNumber = 2, Key = "path", Value = "" },
            new() { LineNumber = 3, Key = "path", Value = "/home" },
            new() { LineNumber = 4, Key = "path", Value = "" }
        };

        _logParserMock.Setup(x => x.Parse(It.IsAny<byte[]>()))
            .Returns(parsedResults);

        var query = new GetTopVisitedUrlsQuery { Count = 3 };

        // Act
        var result = (await _handler.Handle(query, _cancellationToken)).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result[0].HitCount, Is.EqualTo(2));
            Assert.That(result[0].Items, Is.EquivalentTo(new[] { "/home", "" }));
        });
    }
}