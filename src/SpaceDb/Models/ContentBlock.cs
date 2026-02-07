namespace SpaceDb.Models
{
    /// <summary>
    /// Represents a block of content that fits within embedder size limits
    /// Blocks are intermediate nodes between Resources and Fragments
    /// </summary>
    public class ContentBlock
    {
        /// <summary>
        /// Block content (concatenation of fragments within max size)
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Block type
        /// </summary>
        public string Type { get; set; } = "block";

        /// <summary>
        /// Order/index in the resource
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Fragments within this block
        /// </summary>
        public List<ContentFragment> Fragments { get; set; } = new();

        /// <summary>
        /// Additional metadata for the block
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
