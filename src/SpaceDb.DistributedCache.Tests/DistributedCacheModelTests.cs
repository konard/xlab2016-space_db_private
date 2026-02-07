using Xunit;
using SpaceDb.DistributedCache;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace SpaceDb.DistributedCache.Tests;

[Collection("Sequential")] // Disable parallel execution due to static cache
public class DistributedCacheModelTests : IDisposable
{
    private readonly DistributedCacheModel _cache;

    public DistributedCacheModelTests()
    {
        _cache = new DistributedCacheModel();
        _cache.Clear(); // Ensure clean state for each test
    }

    public void Dispose()
    {
        _cache.Clear(); // Clean up after each test
    }

    [Fact]
    public async Task Put_StoresAndRetrievesValue_Successfully()
    {
        // Arrange
        var key = "test_key_1";
        var expectedValue = "test_value";
        var liveTime = TimeSpan.FromSeconds(5);

        // Act
        var result = await _cache.Put(key, liveTime, Task.FromResult(expectedValue));

        // Assert
        Assert.Equal(expectedValue, result);

        var retrievedValue = await _cache.Get<string>(key);
        Assert.Equal(expectedValue, retrievedValue);
    }

    [Fact]
    public async Task Put_ReturnsCachedValue_WhenCalledMultipleTimes()
    {
        // Arrange
        var key = "test_key_2";
        var liveTime = TimeSpan.FromSeconds(5);
        var callCount = 0;

        async Task<int> GetValueAsync()
        {
            await Task.Delay(10); // Simulate async work
            return Interlocked.Increment(ref callCount);
        }

        // Act
        var result1 = await _cache.Put(key, liveTime, GetValueAsync());
        var result2 = await _cache.Put(key, liveTime, GetValueAsync());
        var result3 = await _cache.Put(key, liveTime, GetValueAsync());

        // Assert
        Assert.Equal(1, result1);
        Assert.Equal(1, result2); // Should return cached value
        Assert.Equal(1, result3); // Should return cached value
        Assert.Equal(1, callCount); // GetValueAsync should only be called once
    }

    [Fact]
    public async Task Get_ReturnsNull_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = "non_existent_key";

        // Act
        var result = await _cache.Get<string>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Get_ReturnsNull_WhenValueExpired()
    {
        // Arrange
        var key = "test_key_3";
        var value = "test_value";
        var liveTime = TimeSpan.FromMilliseconds(100);

        // Act
        await _cache.Put(key, liveTime, Task.FromResult(value));
        await Task.Delay(200); // Wait for expiration
        var result = await _cache.Get<string>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Put_WithAsyncGetFalse_RefreshesValueSynchronously_WhenExpired()
    {
        // Arrange
        var key = "test_key_4";
        var liveTime = TimeSpan.FromMilliseconds(100);
        var callCount = 0;

        async Task<int> GetValueAsync()
        {
            await Task.Delay(10);
            return Interlocked.Increment(ref callCount);
        }

        // Act
        var result1 = await _cache.Put(key, liveTime, GetValueAsync(), asyncGet: false);
        await Task.Delay(200); // Wait for expiration
        var result2 = await _cache.Put(key, liveTime, GetValueAsync(), asyncGet: false);

        // Assert
        Assert.Equal(1, result1);
        Assert.Equal(2, result2); // Should fetch new value
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Put_WithAsyncGetTrue_ReturnsStaleValue_WhileRefreshingInBackground()
    {
        // Arrange
        var key = "test_key_5";
        var liveTime = TimeSpan.FromMilliseconds(500); // Longer TTL so value doesn't expire during test
        var callCount = 0;

        async Task<int> GetValueAsync()
        {
            await Task.Delay(50); // Simulate slow fetch
            return Interlocked.Increment(ref callCount);
        }

        // Act
        var result1 = await _cache.Put(key, liveTime, GetValueAsync(), asyncGet: true);
        await Task.Delay(150); // Wait for expiration (TTL is 500ms, so wait 150ms - still valid)

        // Manually expire by waiting longer
        await Task.Delay(400); // Total 550ms - now expired

        // This should return stale value (1) immediately
        var result2 = await _cache.Put(key, liveTime, GetValueAsync(), asyncGet: true);

        // Wait for background refresh to complete (50ms delay + some buffer)
        await Task.Delay(100);

        // This should return refreshed value (2) - it's fresh because liveTime is 500ms
        var result3 = await _cache.Get<int>(key);

        // Assert
        Assert.Equal(1, result1);
        Assert.Equal(1, result2); // Stale value returned immediately
        Assert.Equal(2, result3); // Background refresh completed
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Put_IsThreadSafe_UnderConcurrentAccess()
    {
        // Arrange
        var key = "test_key_6";
        var liveTime = TimeSpan.FromSeconds(10);
        var callCount = 0;

        // Create a single task that will be used by all Put calls
        var getValueTask = Task.Run(async () =>
        {
            await Task.Delay(10);
            return Interlocked.Increment(ref callCount);
        });

        // Act - All threads use the same task
        var tasks = new Task<int>[100];
        for (int i = 0; i < 100; i++)
        {
            tasks[i] = _cache.Put(key, liveTime, getValueTask);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r => Assert.Equal(1, r)); // All should get the same cached value
        Assert.Equal(1, callCount); // GetValueAsync should only be called once
    }

    [Fact]
    public async Task Put_HandlesMultipleKeys_Independently()
    {
        // Arrange
        var key1 = "test_key_7a";
        var key2 = "test_key_7b";
        var value1 = "value1";
        var value2 = "value2";
        var liveTime = TimeSpan.FromSeconds(5);

        // Act
        var result1 = await _cache.Put(key1, liveTime, Task.FromResult(value1));
        var result2 = await _cache.Put(key2, liveTime, Task.FromResult(value2));

        // Assert
        Assert.Equal(value1, result1);
        Assert.Equal(value2, result2);

        var retrieved1 = await _cache.Get<string>(key1);
        var retrieved2 = await _cache.Get<string>(key2);

        Assert.Equal(value1, retrieved1);
        Assert.Equal(value2, retrieved2);
    }

    [Fact]
    public async Task Put_WorksWithComplexTypes()
    {
        // Arrange
        var key = "test_key_8_complex_" + Guid.NewGuid().ToString(); // Make unique to avoid cross-test pollution
        var value = new TestData { Id = 123, Name = "Test", Items = new[] { "A", "B", "C" } };
        var liveTime = TimeSpan.FromSeconds(5);

        // Act
        var result = await _cache.Put(key, liveTime, Task.FromResult(value));

        // Assert Put result
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
        Assert.Equal(value.Items, result.Items);

        // Small delay to ensure value is fully stored
        await Task.Delay(10);

        var retrieved = await _cache.Get<TestData>(key);

        // More detailed assertion
        if (retrieved == null)
        {
            // This shouldn't happen - let's fail with a message
            throw new System.Exception($"Retrieved value is null for key '{key}'. Cache might have been cleared or value expired unexpectedly.");
        }

        Assert.Equal(value.Id, retrieved.Id);
        Assert.Equal(value.Name, retrieved.Name);
        Assert.Equal(value.Items, retrieved.Items);
    }

    [Fact]
    public async Task GetPutStatistics_TracksHitsAndRps()
    {
        // Arrange
        var key = "test_key_9";
        var value = "test_value";
        var liveTime = TimeSpan.FromSeconds(5);

        // Act - Perform multiple Put operations
        await _cache.Put(key + "1", liveTime, Task.FromResult(value));
        await _cache.Put(key + "2", liveTime, Task.FromResult(value));
        await _cache.Put(key + "3", liveTime, Task.FromResult(value));

        var stats = _cache.GetPutStatistics();

        // Assert
        Assert.True(stats.HitsCount >= 3);
        Assert.True(stats.Rps >= 0);
    }

    [Fact]
    public async Task GetGetStatistics_TracksHitsAndRps()
    {
        // Arrange
        var key = "test_key_10";
        var value = "test_value";
        var liveTime = TimeSpan.FromSeconds(5);

        // Put a value first
        await _cache.Put(key, liveTime, Task.FromResult(value));

        // Act - Perform multiple Get operations
        await _cache.Get<string>(key);
        await _cache.Get<string>(key);
        await _cache.Get<string>(key);

        var stats = _cache.GetGetStatistics();

        // Assert
        Assert.True(stats.HitsCount >= 3);
        Assert.True(stats.Rps >= 0);
    }

    [Fact]
    public async Task Statistics_CalculateRps_Correctly()
    {
        // Arrange
        var key = "test_key_11";
        var value = "test_value";
        var liveTime = TimeSpan.FromSeconds(5);

        // Act - Get baseline
        var baselineStats = _cache.GetPutStatistics();

        // Perform operations
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            await _cache.Put(key + i, liveTime, Task.FromResult(value + i));
        }
        stopwatch.Stop();

        await Task.Delay(10); // Small delay to ensure timing
        var stats = _cache.GetPutStatistics();

        // Assert
        Assert.True(stats.HitsCount >= baselineStats.HitsCount + 100);
        Assert.True(stats.Rps > 0); // Should have non-zero RPS
    }

    [Fact]
    public async Task Clear_RemovesAllCachedEntries()
    {
        // Arrange
        var key1 = "test_key_12a";
        var key2 = "test_key_12b";
        var value = "test_value";
        var liveTime = TimeSpan.FromSeconds(5);

        await _cache.Put(key1, liveTime, Task.FromResult(value));
        await _cache.Put(key2, liveTime, Task.FromResult(value));

        // Act
        _cache.Clear();

        // Assert
        var result1 = await _cache.Get<string>(key1);
        var result2 = await _cache.Get<string>(key2);

        Assert.Null(result1);
        Assert.Null(result2);
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string[] Items { get; set; } = Array.Empty<string>();
    }
}
