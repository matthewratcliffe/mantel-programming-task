using Application.Interfaces;
using Application.LogParse.Base;
using Moq;

namespace Test.UnitTests.Application.LogParse;

public class LogParserBaseTests
{
    private Mock<IFileReader> _fileReaderMock;
    private Mock<IApplicationLifetime> _appLifetimeMock;
    private LogParseBase _sut;
    private const string DefaultFileName = "programming-task-example-data.log";
    private const string CustomFileName = "custom.log";

    [SetUp]
    public void Setup()
    {
        _fileReaderMock = new Mock<IFileReader>();
        _appLifetimeMock = new Mock<IApplicationLifetime>();
        _sut = new LogParseBase(_fileReaderMock.Object, _appLifetimeMock.Object);
    }

    [Test]
    public void GetLogFilePath_ShouldCombineCurrentDirectoryWithFileName()
    {
        // Arrange
        var expectedPath = Path.Combine(Directory.GetCurrentDirectory(), CustomFileName);

        // Act
        var result = LogParseBase.GetLogFilePath(CustomFileName);

        // Assert
        Assert.That(result, Is.EqualTo(expectedPath));
    }

    [Test]
    public async Task GetLogFileContents_WithDefaultFileName_ShouldReadCorrectFile()
    {
        // Arrange
        var expectedBytes = new byte[] { 1, 2, 3 };
        var expectedPath = Path.Combine(Directory.GetCurrentDirectory(), DefaultFileName);
        _fileReaderMock.Setup(x => x.ReadAllBytes(expectedPath)).ReturnsAsync(expectedBytes);

        // Act
        var result = await _sut.GetLogFileContents();

        // Assert
        Assert.That(result, Is.EqualTo(expectedBytes));
        _fileReaderMock.Verify(x => x.ReadAllBytes(expectedPath), Times.Once);
    }

    [Test]
    public async Task GetLogFileContents_WithCustomFileName_ShouldReadCorrectFile()
    {
        // Arrange
        var expectedBytes = new byte[] { 1, 2, 3 };
        var expectedPath = Path.Combine(Directory.GetCurrentDirectory(), CustomFileName);
        _fileReaderMock.Setup(x => x.ReadAllBytes(expectedPath)).ReturnsAsync(expectedBytes);

        // Act
        var result = await _sut.GetLogFileContents(CustomFileName);

        // Assert
        Assert.That(result, Is.EqualTo(expectedBytes));
        _fileReaderMock.Verify(x => x.ReadAllBytes(expectedPath), Times.Once);
    }

    [Test]
    public async Task GetLogFileContents_WhenFileEmpty_ShouldExitApplication()
    {
        // Arrange
        var expectedPath = Path.Combine(Directory.GetCurrentDirectory(), DefaultFileName);
        _fileReaderMock.Setup(x => x.ReadAllBytes(expectedPath)).ReturnsAsync([]);

        // Act & Assert
        await _sut.GetLogFileContents(CustomFileName);
        _appLifetimeMock.Verify(s => s.Exit(), Times.Exactly(1));
    }
    
}