using Application.Interfaces;
using Domain;
using Infrastructure.Services.FileReader;
using Moq;

namespace Test.UnitTests.Infrastructure;

[TestFixture]
public class FileReaderTests
{
    private Mock<IVirusScanServiceFactory> _mockVirusScanFactory;
    private Mock<IVirusScan> _mockVirusScanService;
    private FileReader _fileReader;
    private string _testFilePath;
    private byte[] _testFileContent;
        
    [SetUp]
    public void Setup()
    {
        _mockVirusScanService = new Mock<IVirusScan>();
        _mockVirusScanFactory = new Mock<IVirusScanServiceFactory>();
            
        // Setup the factory to return our mock scan service
        _mockVirusScanFactory.Setup(f => f.Create())
            .Returns(_mockVirusScanService.Object);
            
        _fileReader = new FileReader(_mockVirusScanFactory.Object);
            
        // Create a temporary test file
        _testFilePath = Path.GetTempFileName();
        _testFileContent = "This is test content"u8.ToArray();
        File.WriteAllBytes(_testFilePath, _testFileContent);
    }
        
    [TearDown]
    public void Cleanup()
    {
        // Delete the temporary file
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
        
    [Test]
    public async Task ReadAllBytes_ShouldReturnFileBytes_WhenFileIsClean()
    {
        // Arrange
        var cleanResult = new VirusScanResult(true);
            
        // Fix ReturnsAsync issue by using Returns with Task.FromResult
        _mockVirusScanService.Setup(vs => vs.Scan(It.IsAny<byte[]>()))
            .Returns(Task.FromResult(cleanResult));
            
        // Act
        var result = await _fileReader.ReadAllBytes(_testFilePath);
            
        // Assert
        Assert.That(result, Is.EqualTo(_testFileContent));
        _mockVirusScanService.Verify(vs => vs.Scan(It.IsAny<byte[]>()), Times.Once);
    }
        
    [Test]
    public async Task ReadAllBytes_ShouldReturnEmptyArray_WhenFileIsInfected()
    {
        // Arrange
        var infectedResult = new VirusScanResult(false);
            
        _mockVirusScanService.Setup(vs => vs.Scan(It.IsAny<byte[]>()))
            .Returns(Task.FromResult(infectedResult));
            
        // Act
        var result = await _fileReader.ReadAllBytes(_testFilePath);
            
        // Assert
        Assert.That(result, Is.Empty);
        _mockVirusScanService.Verify(vs => vs.Scan(It.IsAny<byte[]>()), Times.Once);
    }
        
    [Test]
    public async Task ReadAllBytes_ShouldNotScanAgain_WhenSameFileIsReadTwice()
    {
        // Arrange
        var cleanResult = new VirusScanResult(true);
            
        _mockVirusScanService.Setup(vs => vs.Scan(It.IsAny<byte[]>()))
            .Returns(Task.FromResult(cleanResult));
            
        // Act
        await _fileReader.ReadAllBytes(_testFilePath); // First read
        await _fileReader.ReadAllBytes(_testFilePath); // Second read
            
        // Assert
        _mockVirusScanService.Verify(vs => vs.Scan(It.IsAny<byte[]>()), Times.Once);
        _mockVirusScanFactory.Verify(f => f.Create(), Times.Once);
    }
        
    [Test]
    public async Task ReadAllBytes_ShouldScanAgain_WhenFileContentChanges()
    {
        // Arrange
        var cleanResult = new VirusScanResult(true);
            
        _mockVirusScanService.Setup(vs => vs.Scan(It.IsAny<byte[]>()))
            .Returns(Task.FromResult(cleanResult));
            
        // Act
        await _fileReader.ReadAllBytes(_testFilePath); // First read
            
        // Change file content
        var newContent = "This content has changed"u8.ToArray();
        await File.WriteAllBytesAsync(_testFilePath, newContent);
            
        await _fileReader.ReadAllBytes(_testFilePath); // Second read
            
        // Assert
        _mockVirusScanService.Verify(vs => vs.Scan(It.IsAny<byte[]>()), Times.Exactly(2));
        _mockVirusScanFactory.Verify(f => f.Create(), Times.Exactly(2));
    }
        
    [Test]
    public void ReadAllBytes_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            
        // Act & Assert
        Assert.ThrowsAsync<FileNotFoundException>(async () => 
            await _fileReader.ReadAllBytes(nonExistentFile));
    }
        
    [Test]
    public void GetSha256Hash_ShouldReturnSameHashForSameContent()
    {
        // Arrange
        var content1 = "Test content"u8.ToArray();
        var content2 = "Test content"u8.ToArray();
            
        // Act
        var hash1 = FileReader.GetSha256Hash(content1);
        var hash2 = FileReader.GetSha256Hash(content2);
            
        // Assert
        Assert.That(hash1, Is.EqualTo(hash2));
    }
        
    [Test]
    public void GetSha256Hash_ShouldReturnDifferentHashForDifferentContent()
    {
        // Arrange
        var content1 = "Test content 1"u8.ToArray();
        var content2 = "Test content 2"u8.ToArray();
            
        // Act
        var hash1 = FileReader.GetSha256Hash(content1);
        var hash2 = FileReader.GetSha256Hash(content2);
            
        // Assert
        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }
        
    [Test]
    public async Task ReadAllBytes_ShouldOutputConsoleMessages_WhenScanningFiles()
    {
        // Arrange
        var cleanResult = new VirusScanResult(true);
            
        _mockVirusScanService.Setup(vs => vs.Scan(It.IsAny<byte[]>()))
            .Returns(Task.FromResult(cleanResult));
            
        var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);
            
        // Act
        await _fileReader.ReadAllBytes(_testFilePath);
            
        // Assert
        var output = consoleOutput.ToString();
        Assert.Multiple(() => {
            Assert.That(output, Does.Contain("Reading file:"));
            Assert.That(output, Does.Contain("Scanning for Viruses, Please Wait ...."));
            Assert.That(output, Does.Contain("Passed ✓"));
        });
            
        // Clean up
        Console.SetOut(Console.Out);
    }
        
    [Test]
    public async Task ReadAllBytes_ShouldShowPreviouslyPassedMessage_WhenFileUnchanged()
    {
        // Arrange
        var cleanResult = new VirusScanResult(true);
            
        _mockVirusScanService.Setup(vs => vs.Scan(It.IsAny<byte[]>()))
            .Returns(Task.FromResult(cleanResult));
            
        // First read to cache the result
        await _fileReader.ReadAllBytes(_testFilePath);
            
        // Setup console capture for second read
        var consoleOutput = new StringWriter();
        Console.SetOut(consoleOutput);
            
        // Act - second read of same file
        await _fileReader.ReadAllBytes(_testFilePath);
            
        // Assert
        var output = consoleOutput.ToString();
        Assert.That(output, Does.Contain("Previously Passed, No Changes Detected ✓"));
            
        // Clean up
        Console.SetOut(Console.Out);
    }
}