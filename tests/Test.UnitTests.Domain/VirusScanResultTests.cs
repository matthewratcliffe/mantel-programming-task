using Domain;

namespace Test.UnitTests.Domain;

[TestFixture]
public class VirusScanResultTests
{
    [Test]
    public void Constructor_WithOnlyIsClean_InitializesCorrectly()
    {
        // Arrange & Act
        var result = new VirusScanResult(true);
            
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsClean, Is.True);
            Assert.That(result.EnginesUsed, Is.Null);
            Assert.That(result.Message, Is.Null);
        });
    }
        
    [Test]
    public void Constructor_WithAllParameters_InitializesCorrectly()
    {
        // Arrange
        var engines = new List<string> { "Engine1", "Engine2" };
        const string message = "Scan completed successfully";
            
        // Act
        var result = new VirusScanResult(false, engines, message);
            
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsClean, Is.False);
            Assert.That(result.EnginesUsed, Is.EqualTo(engines));
            Assert.That(result.Message, Is.EqualTo(message));
        });
    }
        
    [Test]
    public void Constructor_WithIsCleanAndEngines_InitializesCorrectly()
    {
        // Arrange
        var engines = new List<string> { "Engine1", "Engine2" };
            
        // Act
        var result = new VirusScanResult(true, engines);
            
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsClean, Is.True);
            Assert.That(result.EnginesUsed, Is.EqualTo(engines));
            Assert.That(result.Message, Is.Null);
        });
    }
        
    [Test]
    public void Constructor_WithIsCleanAndMessage_InitializesCorrectly()
    {
        // Arrange
        const string message = "Scan completed successfully";
            
        // Act
        var result = new VirusScanResult(true, message: message);
            
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.IsClean, Is.True);
            Assert.That(result.EnginesUsed, Is.Null);
            Assert.That(result.Message, Is.EqualTo(message));
        });
    }
        
    [Test]
    public void Properties_AreReadOnly()
    {
        // Arrange
        var engines = new List<string> { "Engine1" };
        var result = new VirusScanResult(true, engines, "Message");
            
        // Act & Assert
        Assert.DoesNotThrow(() => { engines.Add("Engine2"); });
        Assert.That(result.EnginesUsed?.Count, Is.EqualTo(2));
    }
        
    [Test]
    public void EnginesUsed_CanBeModified_AfterConstruction()
    {
        // Arrange
        var engines = new List<string> { "Engine1" };
        var result = new VirusScanResult(true, engines);
            
        // Act
        result.EnginesUsed?.Add("Engine2");
            
        // Assert
        Assert.That(result.EnginesUsed, Does.Contain("Engine2"));
        Assert.That(result.EnginesUsed?.Count, Is.EqualTo(2));
    }
        
    [Test]
    public void EnginesUsed_IsNullable()
    {
        // Arrange & Act
        var result = new VirusScanResult(true);
            
        // Assert
        Assert.That(result.EnginesUsed, Is.Null);
    }
        
    [Test]
    public void Message_IsNullable()
    {
        // Arrange & Act
        var result = new VirusScanResult(true);
            
        // Assert
        Assert.That(result.Message, Is.Null);
    }
        
    [Test]
    public void Constructor_WithEmptyEnginesList_PreservesEmptyList()
    {
        // Arrange
        var emptyEngines = new List<string>();
            
        // Act
        var result = new VirusScanResult(true, emptyEngines);
            
        // Assert
        Assert.That(result.EnginesUsed, Is.Not.Null);
        Assert.That(result.EnginesUsed, Is.Empty);
    }
        
    [Test]
    public void Constructor_WithEmptyMessage_PreservesEmptyString()
    {
        // Arrange & Act
        var result = new VirusScanResult(true, message: "");
            
        // Assert
        Assert.That(result.Message, Is.Not.Null);
        Assert.That(result.Message, Is.Empty);
    }
}