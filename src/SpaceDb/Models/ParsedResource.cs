namespace SpaceDb.Models
{
    /// <summary>
    /// Represents a parsed resource with its blocks and fragments
    /// </summary>
    public class ParsedResource
    {
        /// <summary>
        /// Resource identifier (filename, url, etc.)
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// Resource type (text, json, etc.)
        /// </summary>
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// Original resource metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Parsed content blocks (intermediate level between resource and fragments)
        /// </summary>
        public List<ContentBlock> Blocks { get; set; } = new();

        /// <summary>
        /// Parsed content fragments (deprecated - kept for backward compatibility)
        /// Use Blocks instead for hierarchical structure
        /// </summary>
        [Obsolete("Use Blocks property instead for hierarchical structure")]
        public List<ContentFragment> Fragments
        {
            get
            {
                // Flatten blocks into fragments for backward compatibility
                return Blocks.SelectMany(b => b.Fragments).ToList();
            }
            set
            {
                // For backward compatibility, convert flat fragments into a single block
                if (value != null && value.Any())
                {
                    Blocks = new List<ContentBlock>
                    {
                        new ContentBlock
                        {
                            Content = string.Join("\n\n", value.Select(f => f.Content)),
                            Type = "legacy_block",
                            Order = 0,
                            Fragments = value
                        }
                    };
                }
            }
        }
    }
}
