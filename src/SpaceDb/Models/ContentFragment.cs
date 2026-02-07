namespace SpaceDb.Models
{
    /// <summary>
    /// Represents a parsed fragment of content
    /// </summary>
    public class ContentFragment
    {
        /// <summary>
        /// Fragment text content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Fragment type (paragraph, json_node, etc.)
        /// </summary>
        public string Type { get; set; } = "text";

        /// <summary>
        /// Order/index in the original content
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Additional metadata for the fragment
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Parent fragment ID (for hierarchical structures like JSON)
        /// </summary>
        public string? ParentKey { get; set; }
    }
}
