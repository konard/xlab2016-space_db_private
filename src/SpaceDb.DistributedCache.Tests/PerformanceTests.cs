using Xunit;
using Xunit.Abstractions;
using SpaceDb.DistributedCache;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

namespace SpaceDb.DistributedCache.Tests;

[Collection("Sequential")] // Disable parallel execution due to static cache
public class PerformanceTests : IDisposable
{
    private readonly DistributedCacheModel _cache;
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _cache = new DistributedCacheModel();
        _cache.Clear();
    }

    public void Dispose()
    {
        _cache.Clear();
    }

    [Fact]
    public async Task Put_Achieves_HighThroughput_CachedValues()
    {
        // Arrange
        var keyPrefix = "perf_test_";
        var value = "test_value";
        var liveTime = TimeSpan.FromSeconds(30);

        // Pre-populate cache
        var key = keyPrefix + "cached";
        await _cache.Put(key, liveTime, Task.FromResult(value));

        // Act - Measure throughput for cached values
        var operationCount = 10000;
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, operationCount)
            .Select(_ => _cache.Put(key, liveTime, Task.FromResult(value)))
            .ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var rps = operationCount / stopwatch.Elapsed.TotalSeconds;
        _output.WriteLine($"Put (cached values) RPS: {rps:N0}");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms for {operationCount} operations");

        Assert.True(rps >= 1000, $"Expected at least 1000 RPS, got {rps:N0}");
    }

    [Fact]
    public async Task Get_Achieves_HighThroughput()
    {
        // Arrange
        var keyPrefix = "perf_test_get_";
        var value = "test_value";
        var liveTime = TimeSpan.FromSeconds(30);

        // Pre-populate cache with multiple keys
        var keyCount = 100;
        for (int i = 0; i < keyCount; i++)
        {
            await _cache.Put(keyPrefix + i, liveTime, Task.FromResult(value + i));
        }

        // Act - Measure Get throughput
        var operationCount = 10000;
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, operationCount)
            .Select(i => _cache.Get<string>(keyPrefix + (i % keyCount)))
            .ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var rps = operationCount / stopwatch.Elapsed.TotalSeconds;
        _output.WriteLine($"Get RPS: {rps:N0}");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms for {operationCount} operations");

        Assert.True(rps >= 1000, $"Expected at least 1000 RPS, got {rps:N0}");
    }

    [Fact]
    public async Task Put_Achieves_HighThroughput_NewValues()
    {
        // Arrange
        var keyPrefix = "perf_test_new_";
        var value = "test_value";
        var liveTime = TimeSpan.FromSeconds(30);

        // Act - Measure throughput for new values (unique keys)
        var operationCount = 5000; // Lower count since each is a new cache entry
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, operationCount)
            .Select(i => _cache.Put(keyPrefix + i, liveTime, Task.FromResult(value + i)))
            .ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var rps = operationCount / stopwatch.Elapsed.TotalSeconds;
        _output.WriteLine($"Put (new values) RPS: {rps:N0}");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms for {operationCount} operations");

        Assert.True(rps >= 1000, $"Expected at least 1000 RPS, got {rps:N0}");
    }

    [Fact]
    public async Task MixedOperations_Achieves_HighThroughput()
    {
        // Arrange
        var keyPrefix = "perf_test_mixed_";
        var value = "test_value";
        var liveTime = TimeSpan.FromSeconds(30);

        // Pre-populate some keys
        for (int i = 0; i < 50; i++)
        {
            await _cache.Put(keyPrefix + i, liveTime, Task.FromResult(value + i));
        }

        // Act - Mix of Get and Put operations
        var operationCount = 10000;
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, operationCount)
            .Select(async i =>
            {
                if (i % 2 == 0)
                {
                    // Get operation
                    await _cache.Get<string>(keyPrefix + (i % 50));
                }
                else
                {
                    // Put operation
                    await _cache.Put(keyPrefix + (i % 50), liveTime, Task.FromResult(value + i));
                }
            })
            .ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var rps = operationCount / stopwatch.Elapsed.TotalSeconds;
        _output.WriteLine($"Mixed operations RPS: {rps:N0}");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms for {operationCount} operations");

        Assert.True(rps >= 1000, $"Expected at least 1000 RPS, got {rps:N0}");
    }

    [Fact]
    public async Task AsyncGet_Achieves_HighThroughput()
    {
        // Arrange
        var keyPrefix = "perf_test_async_";
        var value = "test_value";
        var liveTime = TimeSpan.FromMilliseconds(100); // Short TTL

        // Pre-populate cache
        var key = keyPrefix + "async";
        await _cache.Put(key, liveTime, Task.FromResult(value), asyncGet: true);

        // Wait for expiration
        await Task.Delay(200);

        // Act - Measure throughput with async refresh
        var operationCount = 5000;
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, operationCount)
            .Select(_ => _cache.Put(key, liveTime, Task.FromResult(value), asyncGet: true))
            .ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var rps = operationCount / stopwatch.Elapsed.TotalSeconds;
        _output.WriteLine($"AsyncGet RPS: {rps:N0}");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms for {operationCount} operations");

        Assert.True(rps >= 1000, $"Expected at least 1000 RPS, got {rps:N0}");
    }

    [Fact]
    public async Task Statistics_UpdatesCorrectly_UnderHighLoad()
    {
        // Arrange
        var keyPrefix = "perf_test_stats_";
        var value = "test_value";
        var liveTime = TimeSpan.FromSeconds(30);

        // Get baseline
        var baselinePutStats = _cache.GetPutStatistics();
        var baselineGetStats = _cache.GetGetStatistics();

        // Act - Perform high-load operations
        var operationCount = 5000;

        var putTasks = Enumerable.Range(0, operationCount)
            .Select(i => _cache.Put(keyPrefix + i, liveTime, Task.FromResult(value + i)))
            .ToArray();

        await Task.WhenAll(putTasks);

        var getTasks = Enumerable.Range(0, operationCount)
            .Select(i => _cache.Get<string>(keyPrefix + i))
            .ToArray();

        await Task.WhenAll(getTasks);

        // Small delay to ensure statistics are updated
        await Task.Delay(10);

        var putStats = _cache.GetPutStatistics();
        var getStats = _cache.GetGetStatistics();

        // Assert
        _output.WriteLine($"Put Statistics - HitsCount: {putStats.HitsCount}, RPS: {putStats.Rps:N0}");
        _output.WriteLine($"Get Statistics - HitsCount: {getStats.HitsCount}, RPS: {getStats.Rps:N0}");

        Assert.True(putStats.HitsCount >= baselinePutStats.HitsCount + operationCount);
        Assert.True(getStats.HitsCount >= baselineGetStats.HitsCount + operationCount);
        Assert.True(putStats.Rps >= 0);
        Assert.True(getStats.Rps >= 0);
    }

    [Fact]
    public async Task ConcurrentAccess_MultipleKeys_HighThroughput()
    {
        // Arrange
        var keyCount = 100;
        var keyPrefix = "perf_test_concurrent_";
        var value = "test_value";
        var liveTime = TimeSpan.FromSeconds(30);

        // Act - Concurrent access to multiple keys
        var operationCount = 10000;
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, operationCount)
            .Select(i =>
            {
                var key = keyPrefix + (i % keyCount);
                return _cache.Put(key, liveTime, Task.FromResult(value + i));
            })
            .ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var rps = operationCount / stopwatch.Elapsed.TotalSeconds;
        _output.WriteLine($"Concurrent multi-key RPS: {rps:N0}");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms for {operationCount} operations across {keyCount} keys");

        Assert.True(rps >= 1000, $"Expected at least 1000 RPS, got {rps:N0}");
    }
}
