using Domain;

namespace Test.UnitTests.Domain;

[TestFixture]
public class ParsedLogLineTests
{
    [Test]
    public void ParsedLogLine_WithValidProperties_CreatesInstance()
    {
        // Arrange & Act
        var logLine = new ParsedLogLine
        {
            LineNumber = 1,
            Key = "ip",
            Value = "192.168.1.1"
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(logLine, Is.Not.Null);
            Assert.That(logLine.LineNumber, Is.EqualTo(1));
            Assert.That(logLine.Key, Is.EqualTo("ip"));
            Assert.That(logLine.Value, Is.EqualTo("192.168.1.1"));
        });
    }

    [Test] 
    public void ParsedLogLine_WithNullValue_AllowsNullValue()
    {
        // Arrange & Act
        var logLine = new ParsedLogLine
        {
            LineNumber = 1,
            Key = "ip",
            Value = null
        };

        // Assert
        Assert.That(logLine.Value, Is.Null);
    }

    [Test]
    public void ParsedLogLine_WhenCopiedWithWith_CreatesNewInstanceWithUpdatedValues()
    {
        // Arrange
        var original = new ParsedLogLine
        {
            LineNumber = 1,
            Key = "ip",
            Value = "192.168.1.1"
        };

        // Act
        var copy = original with { Value = "10.0.0.1" };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.Not.SameAs(original));
            Assert.That(copy.LineNumber, Is.EqualTo(1));
            Assert.That(copy.Key, Is.EqualTo("ip"));
            Assert.That(copy.Value, Is.EqualTo("10.0.0.1"));
            Assert.That(original.Value, Is.EqualTo("192.168.1.1"), "Original should be unchanged");
        });
    }

    [Test]
    public void ParsedLogLine_EqualityComparison_WorksAsExpected()
    {
        // Arrange
        var line1 = new ParsedLogLine
        {
            LineNumber = 1,
            Key = "ip",
            Value = "192.168.1.1"
        };

        var line2 = new ParsedLogLine
        {
            LineNumber = 1,
            Key = "ip",
            Value = "192.168.1.1"
        };

        var line3 = new ParsedLogLine
        {
            LineNumber = 2,
            Key = "ip",
            Value = "192.168.1.1"
        };

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(line1, Is.EqualTo(line2), "Equal records should be equal");
            Assert.That(line1, Is.Not.EqualTo(line3), "Different LineNumber should make records not equal");
            Assert.That(line1 == line2, Is.True, "== operator should work");
            Assert.That(line1 != line3, Is.True, "!= operator should work");
        });
    }

    [Test]
    public void ParsedLogLine_GetHashCode_ReturnsSameValueForEqualRecords()
    {
        // Arrange
        var line1 = new ParsedLogLine
        {
            LineNumber = 1,
            Key = "ip",
            Value = "192.168.1.1"
        };

        var line2 = new ParsedLogLine
        {
            LineNumber = 1,
            Key = "ip",
            Value = "192.168.1.1"
        };

        // Act & Assert
        Assert.That(line1.GetHashCode(), Is.EqualTo(line2.GetHashCode()), 
            "Equal records should have equal hash codes");
    }

    [Test]
    public void ParsedLogLine_ToString_ContainsAllProperties()
    {
        // Arrange
        var logLine = new ParsedLogLine
        {
            LineNumber = 1,
            Key = "ip",
            Value = "192.168.1.1"
        };

        // Act
        var str = logLine.ToString();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(str, Does.Contain("LineNumber = 1"));
            Assert.That(str, Does.Contain("Key = ip"));
            Assert.That(str, Does.Contain("Value = 192.168.1.1"));
        });
    }

    [Test]
    public void ParsedLogLine_Deconstruct_ExtractsProperties()
    {
        // Arrange
        var logLine = new ParsedLogLine
        {
            LineNumber = 1,
            Key = "ip",
            Value = "192.168.1.1"
        };

        // Act
        var lineNumber = logLine.LineNumber;
        var key = logLine.Key;
        var value = logLine.Value;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(lineNumber, Is.EqualTo(1));
            Assert.That(key, Is.EqualTo("ip"));
            Assert.That(value, Is.EqualTo("192.168.1.1"));
        });
    }
}