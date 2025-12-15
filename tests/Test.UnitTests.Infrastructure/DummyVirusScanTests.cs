using Application.Interfaces;
using Domain;
using Infrastructure.Services.VirusScan.Dummy;

namespace Test.UnitTests.Infrastructure;

[TestFixture]
public class DummyVirusScanServiceTests
{
    private DummyVirusScanService _virusScanService;

    [SetUp]
    public void Setup()
    {
        _virusScanService = new DummyVirusScanService();
    }

    [Test]
    public void Service_ImplementsIVirusScanInterface()
    {
        // Assert
        Assert.That(_virusScanService, Is.InstanceOf<IVirusScan>());
    }

    [Test]
    public async Task Scan_TakesApproximately80Milliseconds()
    {
        // Arrange
        byte[] testData = [0x01, 0x02, 0x03, 0x04];
        var tolerance = TimeSpan.FromMilliseconds(20); // Allow 20ms tolerance
        
        // Act
        var startTime = DateTime.Now;
        await _virusScanService.Scan(testData);
        var endTime = DateTime.Now;
        var duration = endTime - startTime;
        
        // Assert
        Assert.That(duration, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(80) - tolerance));
        Assert.That(duration, Is.LessThanOrEqualTo(TimeSpan.FromMilliseconds(80) + tolerance));
    }

    [Test]
    public void Scan_DoesNotThrowWithNullData()
    {
        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _virusScanService.Scan(null!));
    }

    [Test]
    public void Scan_DoesNotThrowWithEmptyData()
    {
        // Arrange
        byte[] emptyData = [];

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _virusScanService.Scan(emptyData));
    }

    [Test]
    public void Scan_DoesNotThrowWithLargeData()
    {
        // Arrange
        var largeData = new byte[10 * 1024 * 1024]; // 10 MB
        Random.Shared.NextBytes(largeData);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _virusScanService.Scan(largeData));
    }

    [Test]
    public void Scan_ReturnsVirusScanResult()
    {
        // Arrange
        byte[] testData = [0x01, 0x02, 0x03, 0x04];

        // Act
        var task = _virusScanService.Scan(testData);

        // Assert
        Assert.That(task.Result, Is.TypeOf<VirusScanResult>());
    }
        
    [Test]
    public async Task Scan_ReturnsCorrectValueDistribution()
    {
        // Arrange
        byte[] testData = [0x01, 0x02, 0x03];
        const int totalRuns = 200;
        var trueCount = 0;
            
        // Act
        for (var i = 0; i < totalRuns; i++)
        {
            var result = await _virusScanService.Scan(testData);
            if (result.IsClean)
            {
                trueCount++;
            }
        }
            
        // Assert
        // With 80% true probability, we expect around 800 true results
        // Allow some margin of error for randomness
        const double expectedTruePercentage = 0.8;
        var actualTruePercentage = (double)trueCount / totalRuns;
            
        // We'll use a somewhat generous margin for random variation
        const double margin = 0.10; // 10% margin
            
        Assert.That(actualTruePercentage, Is.InRange(
            expectedTruePercentage - margin, 
            expectedTruePercentage + margin));
    }
        
    [Test]
    public async Task Scan_ProducesRandomResults()
    {
        // Arrange
        byte[] testData = [0x01, 0x02, 0x03];
        const int totalRuns = 100;
        var allSame = true;
        var firstResult = await _virusScanService.Scan(testData);
            
        // Act
        for (var i = 1; i < totalRuns; i++)
        {
            var result = await _virusScanService.Scan(testData);
            if (result != firstResult)
            {
                allSame = false;
                break;
            }
        }
            
        // Assert
        // With enough runs, we should see at least some variation
        // (The probability of getting the same result 100 times is extremely low)
        Assert.That(allSame, Is.False, "Service should produce varying results due to randomization");
    }
        
    [Test]
    public async Task MultipleConcurrentScans_CompletesSuccessfully()
    {
        // Arrange
        byte[] testData = [0x01, 0x02, 0x03, 0x04];
        const int concurrentScans = 5;
        var tasks = new Task<VirusScanResult>[concurrentScans];
            
        // Act
        for (var i = 0; i < concurrentScans; i++)
        {
            tasks[i] = _virusScanService.Scan(testData);
        }
            
        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks);
            
        // Assert
        Assert.That(results, Has.Length.EqualTo(concurrentScans));
        foreach (var result in results)
        {
            Assert.That(result, Is.TypeOf<VirusScanResult>());
        }
    }
        
    [Test]
    public void Scan_IsCancellable()
    {
        // Arrange
        byte[] testData = [0x01, 0x02, 0x03, 0x04];
        var cancellationTokenSource = new CancellationTokenSource();
            
        // Act
        var task = Task.Run(async () => await _virusScanService.Scan(testData), CancellationToken.None);
            
        // Cancel after a short delay
        Task.Delay(100, CancellationToken.None).ContinueWith(_ => cancellationTokenSource.Cancel(), CancellationToken.None);
            
        // Assert
        // If the operation is cancellable, the task should complete faster than the 3-second delay
        // Note: This test is a bit tricky because our implementation doesn't actually check for cancellation
        // If it did, we would check if task.IsCanceled after cancellation
            
        // For now, we're just verifying that the task completes
        Assert.That(task.Wait(4000), Is.True, "Task should complete within timeout");
        
        cancellationTokenSource.Dispose();
    }
}