using SpaceDb.Models;
using System.Text.RegularExpressions;

namespace SpaceDb.Services.Parsers
{
    /// <summary>
    /// Parser for plain text content - splits into paragraphs
    /// </summary>
    public class TextPayloadParser : PayloadParserBase
    {
        private readonly int _minParagraphLength;
        private readonly int _maxParagraphLength;

        public override string ContentType => "text";

        public TextPayloadParser(
            ILogger<TextPayloadParser> logger,
            int minParagraphLength = 50,
            int maxParagraphLength = 2000,
            int maxBlockSize = 8000) : base(logger, maxBlockSize)
        {
            _minParagraphLength = minParagraphLength;
            _maxParagraphLength = maxParagraphLength;
        }

        public override async Task<ParsedResource> ParseAsync(
            string payload,
            string resourceId,
            Dictionary<string, object>? metadata = null)
        {
            _logger.LogInformation("Parsing text payload for resource {ResourceId}", resourceId);

            var result = new ParsedResource
            {
                ResourceId = resourceId,
                ResourceType = ContentType,
                Metadata = CreateMetadata(metadata)
            };

            // First, create fragments as before
            var fragments = new List<ContentFragment>();

            // Split by double newlines (paragraph separator)
            var rawParagraphs = Regex.Split(payload, @"\n\s*\n|\r\n\s*\r\n")
                .Select(p => NormalizeText(p))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            int order = 0;
            var paragraphBuffer = new List<string>(); // Buffer for merging short paragraphs

            foreach (var paragraph in rawParagraphs)
            {
                // If paragraph is short, add to buffer
                if (paragraph.Length < _minParagraphLength)
                {
                    _logger.LogDebug("Adding short paragraph to buffer: {Length} chars", paragraph.Length);
                    paragraphBuffer.Add(paragraph);

                    // Check if buffered content is now long enough
                    var bufferedContent = string.Join("\n\n", paragraphBuffer);
                    if (bufferedContent.Length >= _minParagraphLength)
                    {
                        // Process buffered content
                        if (bufferedContent.Length > _maxParagraphLength)
                        {
                            var chunks = await SplitLongParagraphAsync(bufferedContent);
                            foreach (var chunk in chunks)
                            {
                                fragments.Add(CreateFragment(chunk, order++));
                            }
                        }
                        else
                        {
                            fragments.Add(CreateFragment(bufferedContent, order++));
                        }
                        paragraphBuffer.Clear();
                    }
                    continue;
                }

                // Flush buffer before processing long paragraph
                if (paragraphBuffer.Count > 0)
                {
                    var bufferedContent = string.Join("\n\n", paragraphBuffer);
                    fragments.Add(CreateFragment(bufferedContent, order++));
                    paragraphBuffer.Clear();
                }

                // Split long paragraphs into chunks
                if (paragraph.Length > _maxParagraphLength)
                {
                    var chunks = await SplitLongParagraphAsync(paragraph);
                    foreach (var chunk in chunks)
                    {
                        fragments.Add(CreateFragment(chunk, order++));
                    }
                }
                else
                {
                    fragments.Add(CreateFragment(paragraph, order++));
                }
            }

            // Flush any remaining buffered content
            if (paragraphBuffer.Count > 0)
            {
                var bufferedContent = string.Join("\n\n", paragraphBuffer);
                fragments.Add(CreateFragment(bufferedContent, order++));
                _logger.LogDebug("Flushed final buffer with {Count} short paragraphs", paragraphBuffer.Count);
            }

            // Now group fragments into blocks
            result.Blocks = CreateBlocksFromFragments(fragments);

            result.Metadata["total_fragments"] = fragments.Count;
            result.Metadata["total_blocks"] = result.Blocks.Count;
            result.Metadata["total_length"] = payload.Length;

            _logger.LogInformation("Parsed {FragmentCount} fragments into {BlockCount} blocks from resource {ResourceId}",
                fragments.Count, result.Blocks.Count, resourceId);

            return await Task.FromResult(result);
        }

        private ContentFragment CreateFragment(string content, int order)
        {
            return new ContentFragment
            {
                Content = content,
                Type = "paragraph",
                Order = order,
                Metadata = new Dictionary<string, object>
                {
                    ["length"] = content.Length,
                    ["word_count"] = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                }
            };
        }

        private async Task<List<string>> SplitLongParagraphAsync(string paragraph)
        {
            var chunks = new List<string>();

            // Try to split by sentences first
            var sentences = Regex.Split(paragraph, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var currentChunk = new List<string>();
            int currentLength = 0;

            foreach (var sentence in sentences)
            {
                if (currentLength + sentence.Length > _maxParagraphLength && currentChunk.Count > 0)
                {
                    // Flush current chunk
                    chunks.Add(string.Join(" ", currentChunk));
                    currentChunk.Clear();
                    currentLength = 0;
                }

                currentChunk.Add(sentence);
                currentLength += sentence.Length;
            }

            // Add remaining sentences
            if (currentChunk.Count > 0)
            {
                chunks.Add(string.Join(" ", currentChunk));
            }

            return await Task.FromResult(chunks);
        }

        public override bool CanParse(string payload)
        {
            // Basic text validation
            return base.CanParse(payload) && payload.Length >= _minParagraphLength;
        }
    }
}
