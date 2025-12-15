using System.Text;
using Infrastructure.Services.LogParser;

namespace Test.UnitTests.Infrastructure;

[TestFixture]
public class RegexParserServiceTests
{
    private RegexParserService _parser;

    [SetUp]
    public void Setup()
    {
        _parser = new RegexParserService();
    }

    [Test]
    public void Parse_EmptyInput_ReturnsEmptyList()
    {
        // Arrange
        var emptyBytes = Encoding.UTF8.GetBytes("");
            
        // Act
        var result = _parser.Parse(emptyBytes);
            
        // Assert
        Assert.That(result, Is.Empty);
    }
        
    [Test]
    public void Parse_ValidLogLine_ExtractsAllFields()
    {
        // Arrange
        const string logLine = "192.168.1.1 - john [10/Oct/2000:13:55:36 -0700] \"GET /index.html HTTP/1.0\" 200 2326 \"http://www.example.com/start.html\" \"Mozilla/4.08 [en] (Win98; I ;Nav)\"";
        var logBytes = Encoding.UTF8.GetBytes(logLine);
            
        // Act
        var results = _parser.Parse(logBytes);
            
        // Assert
        Assert.That(results, Is.Not.Empty);
            
        // Verify all expected fields are extracted
        var resultDictionary = results.ToDictionary(r => r.Key, r => r.Value);

        Assert.Multiple(() =>
        {
            Assert.That(resultDictionary, Contains.Key("ip"));
            Assert.That(resultDictionary["ip"], Is.EqualTo("192.168.1.1"));

            Assert.That(resultDictionary, Contains.Key("ident"));
            Assert.That(resultDictionary["ident"], Is.EqualTo("-"));

            Assert.That(resultDictionary, Contains.Key("authuser"));
            Assert.That(resultDictionary["authuser"], Is.EqualTo("john"));

            Assert.That(resultDictionary, Contains.Key("timestamp"));
            Assert.That(resultDictionary["timestamp"], Is.EqualTo("10/Oct/2000:13:55:36 -0700"));

            Assert.That(resultDictionary, Contains.Key("method"));
            Assert.That(resultDictionary["method"], Is.EqualTo("GET"));

            Assert.That(resultDictionary, Contains.Key("path"));
            Assert.That(resultDictionary["path"], Is.EqualTo("/index.html"));

            Assert.That(resultDictionary, Contains.Key("protocol"));
            Assert.That(resultDictionary["protocol"], Is.EqualTo("HTTP/1.0"));

            Assert.That(resultDictionary, Contains.Key("status"));
            Assert.That(resultDictionary["status"], Is.EqualTo("200"));

            Assert.That(resultDictionary, Contains.Key("bytes"));
            Assert.That(resultDictionary["bytes"], Is.EqualTo("2326"));

            Assert.That(resultDictionary, Contains.Key("referrer"));
            Assert.That(resultDictionary["referrer"], Is.EqualTo("http://www.example.com/start.html"));

            Assert.That(resultDictionary, Contains.Key("agent"));
            Assert.That(resultDictionary["agent"], Is.EqualTo("Mozilla/4.08 [en] (Win98; I ;Nav)"));
        });
    }
        
    [Test]
    public void Parse_InvalidLogLine_MarksAsRaw()
    {
        // Arrange
        const string invalidLog = "This is not a valid log line format";
        var logBytes = Encoding.UTF8.GetBytes(invalidLog);
            
        // Act
        var results = _parser.Parse(logBytes);
            
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].Key, Is.EqualTo("raw"));
            Assert.That(results[0].Value, Is.EqualTo(invalidLog));
            Assert.That(results[0].LineNumber, Is.EqualTo(1));
        });
    }
        
    [Test]
    public void Parse_MultipleLines_AssignsCorrectLineNumbers()
    {
        // Arrange
        const string logContent = 
            "192.168.1.1 - john [10/Oct/2000:13:55:36 -0700] \"GET /index.html HTTP/1.0\" 200 2326 \"http://www.example.com/start.html\" \"Mozilla/4.08 [en] (Win98; I ;Nav)\"\n" +
            "Invalid line\n" +
            "10.0.0.1 - mike [10/Oct/2000:13:56:36 -0700] \"POST /api/data HTTP/1.0\" 201 1024 \"http://www.example.com/form.html\" \"Mozilla/5.0\"";
            
        var logBytes = Encoding.UTF8.GetBytes(logContent);
            
        // Act
        var results = _parser.Parse(logBytes);
            
        // Assert
        var line1Results = results.Where(r => r.LineNumber == 1).ToList();
        var line2Results = results.Where(r => r.LineNumber == 2).ToList();
        var line3Results = results.Where(r => r.LineNumber == 3).ToList();

        Assert.Multiple(() =>
        {
            // Check line 1 (valid)
            Assert.That(line1Results, Is.Not.Empty);
            Assert.That(line1Results.Any(r => r is { Key: "ip", Value: "192.168.1.1" }), Is.True);

            // Check line 2 (invalid)
            Assert.That(line2Results, Has.Count.EqualTo(1));
            Assert.That(line2Results[0].Key, Is.EqualTo("raw"));
            Assert.That(line2Results[0].Value, Is.EqualTo("Invalid line"));

            // Check line 3 (valid)
            Assert.That(line3Results, Is.Not.Empty);
            Assert.That(line3Results.Any(r => r is { Key: "ip", Value: "10.0.0.1" }), Is.True);
            Assert.That(line3Results.Any(r => r is { Key: "method", Value: "POST" }), Is.True);
        });
    }
        
    [Test]
    public void Parse_ComplexPath_ExtractsCorrectly()
    {
        // Arrange
        const string logLine = "192.168.1.1 - - [10/Oct/2000:13:55:36 -0700] \"GET /path/with/query?param1=value&param2=123 HTTP/1.1\" 200 2326 \"-\" \"Mozilla/5.0\"";
        var logBytes = Encoding.UTF8.GetBytes(logLine);
            
        // Act
        var results = _parser.Parse(logBytes);
            
        // Assert
        var pathResult = results.FirstOrDefault(r => r.Key == "path");
            
        Assert.Multiple(() => {
            Assert.That(pathResult, Is.Not.Null);
            Assert.That(pathResult!.Value, Is.EqualTo("/path/with/query?param1=value&param2=123"));
        });
    }
        
    [Test]
    public void Parse_DifferentStatusCodes_ExtractsCorrectly()
    {
        // Arrange
        const string logContent = 
            "192.168.1.1 - - [10/Oct/2000:13:55:36 -0700] \"GET /index.html HTTP/1.0\" 200 2326 \"-\" \"Mozilla/5.0\"\n" +
            "192.168.1.1 - - [10/Oct/2000:13:55:37 -0700] \"GET /notfound.html HTTP/1.0\" 404 500 \"-\" \"Mozilla/5.0\"\n" +
            "192.168.1.1 - - [10/Oct/2000:13:55:38 -0700] \"GET /servererror.html HTTP/1.0\" 500 1000 \"-\" \"Mozilla/5.0\"";
            
        var logBytes = Encoding.UTF8.GetBytes(logContent);
            
        // Act
        var results = _parser.Parse(logBytes);
            
        // Assert
        var statusResults = results.Where(r => r.Key == "status").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(statusResults, Has.Count.EqualTo(3));
            Assert.That(statusResults.Select(r => r.Value), Does.Contain("200"));
            Assert.That(statusResults.Select(r => r.Value), Does.Contain("404"));
            Assert.That(statusResults.Select(r => r.Value), Does.Contain("500"));
        });
    }
        
    [Test]
    public void Parse_WithEmptyReferrerAndUserAgent_ParsesCorrectly()
    {
        // Arrange
        const string logLine = "192.168.1.1 - - [10/Oct/2000:13:55:36 -0700] \"GET /index.html HTTP/1.0\" 200 2326 \"\" \"\"";
        var logBytes = Encoding.UTF8.GetBytes(logLine);
            
        // Act
        var results = _parser.Parse(logBytes);
            
        // Assert
        var referrerResult = results.FirstOrDefault(r => r.Key == "referrer");
        var agentResult = results.FirstOrDefault(r => r.Key == "agent");
            
        Assert.Multiple(() => {
            Assert.That(referrerResult, Is.Not.Null);
            Assert.That(referrerResult!.Value, Is.EqualTo(""));
                
            Assert.That(agentResult, Is.Not.Null);
            Assert.That(agentResult!.Value, Is.EqualTo(""));
        });
    }
        
    [Test]
    public void Parse_SpecialCharactersInPath_ParsesCorrectly()
    {
        // Arrange
        const string logLine = "192.168.1.1 - - [10/Oct/2000:13:55:36 -0700] \"GET /path/with/special/chars/%20%3F%26.html HTTP/1.0\" 200 2326 \"-\" \"Mozilla/5.0\"";
        var logBytes = Encoding.UTF8.GetBytes(logLine);
            
        // Act
        var results = _parser.Parse(logBytes);
            
        // Assert
        var pathResult = results.FirstOrDefault(r => r.Key == "path");

        Assert.Multiple(() =>
        {
            Assert.That(pathResult, Is.Not.Null);
            Assert.That(pathResult!.Value, Is.EqualTo("/path/with/special/chars/%20%3F%26.html"));
        });
    }
        
    [Test]
    public void Parse_MixedValidAndInvalidLines_HandlesCorrectly()
    {
        // Arrange
        const string logContent = 
            "Valid but not matching log format\n" +
            "192.168.1.1 - - [10/Oct/2000:13:55:36 -0700] \"GET /index.html HTTP/1.0\" 200 2326 \"-\" \"Mozilla/5.0\"\n" +
            "Another invalid line\n" +
            "192.168.1.2 - - [10/Oct/2000:13:56:36 -0700] \"POST /api/data HTTP/1.0\" 201 1024 \"-\" \"Mozilla/5.0\"";
            
        var logBytes = Encoding.UTF8.GetBytes(logContent);
            
        // Act
        var results = _parser.Parse(logBytes);
            
        // Assert
        var rawLines = results.Where(r => r.Key == "raw").ToList();
        var validLines = results.Where(r => r.Key == "ip").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(rawLines, Has.Count.EqualTo(2));
            Assert.That(validLines, Has.Count.EqualTo(2));

            Assert.That(rawLines[0].LineNumber, Is.EqualTo(1));
            Assert.That(rawLines[1].LineNumber, Is.EqualTo(3));

            Assert.That(validLines[0].LineNumber, Is.EqualTo(2));
            Assert.That(validLines[1].LineNumber, Is.EqualTo(4));
        });
    }
        
    [Test]
    public void Parse_DifferentEncodings_HandlesProperly()
    {
        // Arrange - Using UTF-8 with a non-ASCII character
        const string logLine = "192.168.1.1 - jöhn [10/Oct/2000:13:55:36 -0700] \"GET /index.html HTTP/1.0\" 200 2326 \"-\" \"Mozilla/5.0\"";
        var utf8Bytes = Encoding.UTF8.GetBytes(logLine);
            
        // Act
        var results = _parser.Parse(utf8Bytes);
            
        // Assert
        var userResult = results.FirstOrDefault(r => r.Key == "authuser");

        Assert.Multiple(() =>
        {
            Assert.That(userResult, Is.Not.Null);
            Assert.That(userResult!.Value, Is.EqualTo("jöhn"));
        });
    }
        
    [Test]
    public void Parse_VeryLargeLogLine_ParsesSuccessfully()
    {
        // Arrange - Create a log line with a very long user agent
        var veryLongUserAgent = new string('a', 5000);
        var logLine = $"192.168.1.1 - - [10/Oct/2000:13:55:36 -0700] \"GET /index.html HTTP/1.0\" 200 2326 \"-\" \"{veryLongUserAgent}\"";
        var logBytes = Encoding.UTF8.GetBytes(logLine);
            
        // Act
        var results = _parser.Parse(logBytes);
            
        // Assert
        var agentResult = results.FirstOrDefault(r => r.Key == "agent");

        Assert.Multiple(() =>
        {
            Assert.That(agentResult, Is.Not.Null);
            Assert.That(agentResult!.Value, Is.EqualTo(veryLongUserAgent));
        });
    }
}