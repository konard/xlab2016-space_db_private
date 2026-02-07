using SpaceDb.Models;

namespace SpaceDb.Services.Parsers
{
    /// <summary>
    /// Base class for payload parsers that split content into blocks and fragments
    /// </summary>
    public abstract class PayloadParserBase
    {
        protected readonly ILogger _logger;
        protected readonly int _maxBlockSize;

        protected PayloadParserBase(ILogger logger, int maxBlockSize = 8000)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxBlockSize = maxBlockSize;
        }

        /// <summary>
        /// Parse payload into structured fragments
        /// </summary>
        /// <param name="payload">Raw payload content</param>
        /// <param name="resourceId">Resource identifier</param>
        /// <param name="metadata">Additional metadata</param>
        /// <returns>Parsed resource with fragments</returns>
        public abstract Task<ParsedResource> ParseAsync(
            string payload,
            string resourceId,
            Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Get the content type this parser handles
        /// </summary>
        public abstract string ContentType { get; }

        /// <summary>
        /// Validate if this parser can handle the given payload
        /// </summary>
        public virtual bool CanParse(string payload)
        {
            return !string.IsNullOrWhiteSpace(payload);
        }

        /// <summary>
        /// Clean and normalize text
        /// </summary>
        protected string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove excessive whitespace
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

            // Trim
            text = text.Trim();

            return text;
        }

        /// <summary>
        /// Create metadata dictionary with common fields
        /// </summary>
        protected Dictionary<string, object> CreateMetadata(
            Dictionary<string, object>? baseMetadata = null)
        {
            var metadata = baseMetadata != null
                ? new Dictionary<string, object>(baseMetadata)
                : new Dictionary<string, object>();

            metadata["parsed_at"] = DateTime.UtcNow;
            metadata["parser_type"] = ContentType;

            return metadata;
        }

        /// <summary>
        /// Group fragments into blocks based on max block size
        /// </summary>
        protected List<ContentBlock> CreateBlocksFromFragments(List<ContentFragment> fragments)
        {
            var blocks = new List<ContentBlock>();
            var currentBlock = new ContentBlock
            {
                Order = 0,
                Type = "block",
                Fragments = new List<ContentFragment>()
            };

            int currentBlockSize = 0;

            foreach (var fragment in fragments)
            {
                var fragmentSize = fragment.Content.Length;

                // If adding this fragment exceeds max block size and current block is not empty, start new block
                if (currentBlockSize + fragmentSize > _maxBlockSize && currentBlock.Fragments.Count > 0)
                {
                    // Finalize current block
                    currentBlock.Content = string.Join("\n\n", currentBlock.Fragments.Select(f => f.Content));
                    currentBlock.Metadata = new Dictionary<string, object>
                    {
                        ["fragment_count"] = currentBlock.Fragments.Count,
                        ["size"] = currentBlockSize
                    };
                    blocks.Add(currentBlock);

                    // Start new block
                    currentBlock = new ContentBlock
                    {
                        Order = blocks.Count,
                        Type = "block",
                        Fragments = new List<ContentFragment>()
                    };
                    currentBlockSize = 0;
                }

                // Add fragment to current block
                currentBlock.Fragments.Add(fragment);
                currentBlockSize += fragmentSize;
            }

            // Add final block if it has any fragments
            if (currentBlock.Fragments.Count > 0)
            {
                currentBlock.Content = string.Join("\n\n", currentBlock.Fragments.Select(f => f.Content));
                currentBlock.Metadata = new Dictionary<string, object>
                {
                    ["fragment_count"] = currentBlock.Fragments.Count,
                    ["size"] = currentBlockSize
                };
                blocks.Add(currentBlock);
            }

            _logger.LogInformation("Created {BlockCount} blocks from {FragmentCount} fragments",
                blocks.Count, fragments.Count);

            return blocks;
        }
    }
}
