namespace SpaceDb.DistributedCache;

/// <summary>
/// Represents statistics for cache operations.
/// </summary>
public class StatisticModel
{
    /// <summary>
    /// Gets or sets the total number of cache hits.
    /// </summary>
    public long HitsCount { get; set; }

    /// <summary>
    /// Gets or sets the current requests per second.
    /// </summary>
    public double Rps { get; set; }
}
