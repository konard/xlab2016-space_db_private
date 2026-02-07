using SpaceDb.Models;

namespace SpaceDb.Services
{
    /// <summary>
    /// Service for parsing content and storing as resource->fragments graph in SpaceDb
    /// </summary>
    public interface IContentParserService
    {
        /// <summary>
        /// Parse and store content as resource (dimension=0) with fragments (dimension=1)
        /// </summary>
        /// <param name="payload">Raw content payload</param>
        /// <param name="resourceId">Resource identifier</param>
        /// <param name="contentType">Content type (text, json, auto)</param>
        /// <param name="singularityId">Singularity namespace</param>
        /// <param name="userId">User owner</param>
        /// <param name="metadata">Additional metadata</param>
        /// <returns>Resource point ID and created fragment IDs</returns>
        Task<ContentParseResult?> ParseAndStoreAsync(
            string payload,
            string resourceId,
            string contentType = "auto",
            long? singularityId = null,
            int? userId = null,
            Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Get available parser types
        /// </summary>
        IEnumerable<string> GetAvailableParserTypes();
    }

    /// <summary>
    /// Result of content parsing and storage
    /// </summary>
    public class ContentParseResult
    {
        /// <summary>
        /// Resource point ID (dimension=0)
        /// </summary>
        public long ResourcePointId { get; set; }

        /// <summary>
        /// Block point IDs (dimension=1, intermediate level)
        /// </summary>
        public List<long> BlockPointIds { get; set; } = new();

        /// <summary>
        /// Fragment point IDs (dimension=1, leaf level)
        /// </summary>
        public List<long> FragmentPointIds { get; set; } = new();

        /// <summary>
        /// Segment IDs connecting resource to blocks and blocks to fragments
        /// </summary>
        public List<long> SegmentIds { get; set; } = new();

        /// <summary>
        /// Parser type used
        /// </summary>
        public string ParserType { get; set; } = string.Empty;

        /// <summary>
        /// Total blocks created
        /// </summary>
        public int TotalBlocks => BlockPointIds.Count;

        /// <summary>
        /// Total fragments created
        /// </summary>
        public int TotalFragments => FragmentPointIds.Count;
    }
}
