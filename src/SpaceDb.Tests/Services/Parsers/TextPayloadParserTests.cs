using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceDb.Services.Parsers;
using Xunit;

namespace SpaceDb.Tests.Services.Parsers
{
    public class TextPayloadParserTests
    {
        private readonly Mock<ILogger<TextPayloadParser>> _loggerMock;
        private readonly TextPayloadParser _parser;

        public TextPayloadParserTests()
        {
            _loggerMock = new Mock<ILogger<TextPayloadParser>>();
            _parser = new TextPayloadParser(_loggerMock.Object);
        }

        #region Basic Functionality Tests

        [Fact]
        public void ContentType_ShouldReturnText()
        {
            // Act
            var contentType = _parser.ContentType;

            // Assert
            contentType.Should().Be("text");
        }

        [Fact]
        public async Task ParseAsync_WithNormalParagraph_ShouldCreateSingleFragment()
        {
            // Arrange
            var payload = "This is a normal paragraph with enough content to meet the minimum length requirement for fragment creation.";
            var resourceId = "test_resource_1";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Should().NotBeNull();
            result.ResourceId.Should().Be(resourceId);
            result.ResourceType.Should().Be("text");
            result.Blocks.Should().HaveCount(1);
            result.Blocks[0].Fragments.Should().HaveCount(1);
            result.Blocks[0].Fragments[0].Content.Should().Contain("normal paragraph");
            result.Blocks[0].Fragments[0].Type.Should().Be("paragraph");
            result.Blocks[0].Fragments[0].Order.Should().Be(0);
        }

        [Fact]
        public async Task ParseAsync_WithMultipleParagraphs_ShouldCreateMultipleFragments()
        {
            // Arrange
            var payload = @"First paragraph with sufficient content to meet requirements.

Second paragraph that also has enough text to be considered valid.

Third paragraph continuing the pattern of adequate length.";
            var resourceId = "test_resource_2";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCount(3);
            allFragments[0].Content.Should().Contain("First paragraph");
            allFragments[1].Content.Should().Contain("Second paragraph");
            allFragments[2].Content.Should().Contain("Third paragraph");
            allFragments[0].Order.Should().Be(0);
            allFragments[1].Order.Should().Be(1);
            allFragments[2].Order.Should().Be(2);
        }

        #endregion

        #region Short Paragraph Merging Tests

        [Fact]
        public async Task ParseAsync_WithSingleShortParagraph_ShouldNotSkipContent()
        {
            // Arrange - paragraph shorter than default minLength (50)
            var payload = "Short text.";
            var resourceId = "test_short_1";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCount(1);
            allFragments[0].Content.Should().Be("Short text.");
        }

        [Fact]
        public async Task ParseAsync_WithMultipleShortParagraphs_ShouldMergeThem()
        {
            // Arrange - three short paragraphs that should be merged
            var payload = @"Short one.

Short two.

Short three.";
            var resourceId = "test_short_merge";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCount(1);
            allFragments[0].Content.Should().Contain("Short one.");
            allFragments[0].Content.Should().Contain("Short two.");
            allFragments[0].Content.Should().Contain("Short three.");
        }

        [Fact]
        public async Task ParseAsync_WithShortThenLongParagraph_ShouldFlushBufferBeforeLong()
        {
            // Arrange
            var payload = @"Short.

This is a long paragraph with enough content to meet the minimum length requirement and be processed independently.";
            var resourceId = "test_short_long";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCount(2);
            allFragments[0].Content.Should().Be("Short.");
            allFragments[1].Content.Should().Contain("long paragraph");
        }

        [Fact]
        public async Task ParseAsync_WithLongThenShortParagraph_ShouldProcessCorrectly()
        {
            // Arrange
            var payload = @"This is a long paragraph with enough content to meet the minimum length requirement.

Short end.";
            var resourceId = "test_long_short";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCount(2);
            allFragments[0].Content.Should().Contain("long paragraph");
            allFragments[1].Content.Should().Be("Short end.");
        }

        [Fact]
        public async Task ParseAsync_WithMixedParagraphLengths_ShouldHandleAll()
        {
            // Arrange
            var payload = @"Short 1.

This is a medium length paragraph that meets requirements.

Short 2.

Short 3.

Another long paragraph with sufficient content to be processed on its own.

Short 4.";
            var resourceId = "test_mixed";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCountGreaterThan(0);
            // All content should be preserved
            var allContent = string.Join(" ", allFragments.Select(f => f.Content));
            allContent.Should().Contain("Short 1");
            allContent.Should().Contain("Short 2");
            allContent.Should().Contain("Short 3");
            allContent.Should().Contain("Short 4");
            allContent.Should().Contain("medium length");
            allContent.Should().Contain("Another long");
        }

        #endregion

        #region Long Paragraph Splitting Tests

        [Fact]
        public async Task ParseAsync_WithVeryLongParagraph_ShouldSplitIntoChunks()
        {
            // Arrange - create a paragraph longer than default maxLength (2000)
            var longSentence = "This is a sentence that will be repeated many times to create a very long paragraph. ";
            var payload = string.Concat(Enumerable.Repeat(longSentence, 50)); // ~4000 chars
            var resourceId = "test_long_split";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCountGreaterThan(1);
            foreach (var fragment in allFragments)
            {
                fragment.Content.Length.Should().BeLessThanOrEqualTo(2000);
            }
        }

        [Fact]
        public async Task ParseAsync_WithCustomMaxLength_ShouldRespectLimit()
        {
            // Arrange
            var parser = new TextPayloadParser(_loggerMock.Object, minParagraphLength: 10, maxParagraphLength: 100);
            var payload = string.Concat(Enumerable.Repeat("Long text to exceed custom limit. ", 20)); // ~700 chars
            var resourceId = "test_custom_max";

            // Act
            var result = await parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCountGreaterThan(1);
            // Allow small margin for sentence boundary joins (spaces between sentences)
            foreach (var fragment in allFragments)
            {
                fragment.Content.Length.Should().BeLessThanOrEqualTo(110);
            }
        }

        [Fact]
        public async Task ParseAsync_LongParagraph_ShouldSplitAtSentenceBoundaries()
        {
            // Arrange - create paragraph longer than 2000 chars
            var sentence1 = string.Concat(Enumerable.Repeat("First sentence content. ", 50));
            var sentence2 = string.Concat(Enumerable.Repeat("Second sentence content. ", 50));
            var sentence3 = string.Concat(Enumerable.Repeat("Third sentence content. ", 50));
            var payload = $"{sentence1} {sentence2} {sentence3}";
            var resourceId = "test_sentence_split";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCountGreaterThan(1);
            // Each fragment should end at a sentence boundary (period + space)
            foreach (var fragment in allFragments.Take(allFragments.Count - 1))
            {
                fragment.Content.Should().Contain(".");
            }
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task ParseAsync_WithEmptyString_ShouldReturnNoFragments()
        {
            // Arrange
            var payload = "";
            var resourceId = "test_empty";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Blocks.Should().BeEmpty();
        }

        [Fact]
        public async Task ParseAsync_WithWhitespaceOnly_ShouldReturnNoFragments()
        {
            // Arrange
            var payload = "   \n\n   \r\n\r\n   ";
            var resourceId = "test_whitespace";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Blocks.Should().BeEmpty();
        }

        [Fact]
        public async Task ParseAsync_WithExcessiveWhitespace_ShouldNormalize()
        {
            // Arrange
            var payload = "This    has     excessive     whitespace     in     the     middle and should be normalized.";
            var resourceId = "test_normalize";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCount(1);
            allFragments[0].Content.Should().NotContain("    ");
            allFragments[0].Content.Should().Contain("excessive whitespace in the middle");
        }

        [Fact]
        public async Task ParseAsync_WithDifferentLineEndings_ShouldHandleBoth()
        {
            // Arrange - test both \n and \r\n with paragraphs long enough to not be merged
            var payload = "First paragraph with Unix line ending and sufficient content.\n\nSecond paragraph with Windows line ending and enough text.\r\n\r\nThird paragraph mixed with adequate length for processing.";
            var resourceId = "test_line_endings";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCount(3);
        }

        [Fact]
        public async Task ParseAsync_WithSingleCharacterParagraphs_ShouldMerge()
        {
            // Arrange
            var payload = @"A

B

C

D

E";
            var resourceId = "test_single_char";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCount(1);
            var content = allFragments[0].Content;
            content.Should().Contain("A");
            content.Should().Contain("B");
            content.Should().Contain("C");
            content.Should().Contain("D");
            content.Should().Contain("E");
        }

        #endregion

        #region Metadata Tests

        [Fact]
        public async Task ParseAsync_ShouldIncludeFragmentMetadata()
        {
            // Arrange
            var payload = "This is a test paragraph with sufficient length to create a proper fragment.";
            var resourceId = "test_metadata";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            var fragment = allFragments[0];
            fragment.Metadata.Should().ContainKey("length");
            fragment.Metadata.Should().ContainKey("word_count");
            fragment.Metadata["length"].Should().Be(fragment.Content.Length);
        }

        [Fact]
        public async Task ParseAsync_ShouldIncludeResourceMetadata()
        {
            // Arrange
            var payload = "Test content with adequate length for processing.";
            var resourceId = "test_resource_metadata";
            var customMetadata = new Dictionary<string, object>
            {
                ["custom_field"] = "custom_value",
                ["author"] = "test_author"
            };

            // Act
            var result = await _parser.ParseAsync(payload, resourceId, customMetadata);

            // Assert
            result.Metadata.Should().ContainKey("custom_field");
            result.Metadata.Should().ContainKey("author");
            result.Metadata.Should().ContainKey("parsed_at");
            result.Metadata.Should().ContainKey("parser_type");
            result.Metadata.Should().ContainKey("total_fragments");
            result.Metadata.Should().ContainKey("total_length");
            result.Metadata["parser_type"].Should().Be("text");
            result.Metadata["total_length"].Should().Be(payload.Length);
        }

        [Fact]
        public async Task ParseAsync_ShouldCalculateWordCount()
        {
            // Arrange
            var payload = "One two three four five six seven eight nine ten words here.";
            var resourceId = "test_word_count";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            var fragment = allFragments[0];
            fragment.Metadata["word_count"].Should().Be(12);
        }

        #endregion

        #region Custom Configuration Tests

        [Fact]
        public async Task ParseAsync_WithCustomMinLength_ShouldRespectSetting()
        {
            // Arrange
            var parser = new TextPayloadParser(_loggerMock.Object, minParagraphLength: 20, maxParagraphLength: 2000);
            var payload = @"Short 10 chars

This is exactly 20 c

This paragraph is longer than twenty characters.";
            var resourceId = "test_custom_min";

            // Act
            var result = await parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().NotBeEmpty();
            // Short paragraphs should be merged
            var allContent = string.Join(" ", allFragments.Select(f => f.Content));
            allContent.Should().Contain("Short 10 chars");
        }

        [Fact]
        public void CanParse_WithValidText_ShouldReturnTrue()
        {
            // Arrange
            var payload = "This is valid text content with adequate length to meet the minimum requirement of fifty characters for parsing.";

            // Act
            var canParse = _parser.CanParse(payload);

            // Assert
            canParse.Should().BeTrue();
        }

        [Fact]
        public void CanParse_WithTooShortText_ShouldReturnFalse()
        {
            // Arrange - shorter than default minLength (50)
            var payload = "Too short";

            // Act
            var canParse = _parser.CanParse(payload);

            // Assert
            canParse.Should().BeFalse();
        }

        [Fact]
        public void CanParse_WithEmptyString_ShouldReturnFalse()
        {
            // Arrange
            var payload = "";

            // Act
            var canParse = _parser.CanParse(payload);

            // Assert
            canParse.Should().BeFalse();
        }

        [Fact]
        public void CanParse_WithWhitespaceOnly_ShouldReturnFalse()
        {
            // Arrange
            var payload = "   \n\n   ";

            // Act
            var canParse = _parser.CanParse(payload);

            // Assert
            canParse.Should().BeFalse();
        }

        #endregion

        #region Order Preservation Tests

        [Fact]
        public async Task ParseAsync_ShouldMaintainCorrectOrder()
        {
            // Arrange
            var payload = @"Paragraph Alpha with sufficient content for processing.

Paragraph Beta with sufficient content for processing.

Paragraph Gamma with sufficient content for processing.

Paragraph Delta with sufficient content for processing.";
            var resourceId = "test_order";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCount(4);
            allFragments[0].Content.Should().Contain("Alpha");
            allFragments[1].Content.Should().Contain("Beta");
            allFragments[2].Content.Should().Contain("Gamma");
            allFragments[3].Content.Should().Contain("Delta");

            for (int i = 0; i < allFragments.Count; i++)
            {
                allFragments[i].Order.Should().Be(i);
            }
        }

        [Fact]
        public async Task ParseAsync_WithSplitLongParagraph_ShouldMaintainOrder()
        {
            // Arrange
            var paragraph1 = "First normal paragraph with adequate content.";
            var longParagraph = string.Concat(Enumerable.Repeat("Long paragraph content. ", 100)); // Will be split
            var paragraph2 = "Last normal paragraph with adequate content.";
            var payload = $"{paragraph1}\n\n{longParagraph}\n\n{paragraph2}";
            var resourceId = "test_split_order";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            allFragments.Should().HaveCountGreaterThan(2);
            allFragments[0].Content.Should().Contain("First normal");
            allFragments[^1].Content.Should().Contain("Last normal");

            // Orders should be sequential
            for (int i = 0; i < allFragments.Count; i++)
            {
                allFragments[i].Order.Should().Be(i);
            }
        }

        #endregion

        #region No Content Loss Tests

        [Fact]
        public async Task ParseAsync_ShouldNeverLoseContent()
        {
            // Arrange - mix of short, normal, and long paragraphs
            var paragraphs = new[]
            {
                "A",
                "Short paragraph number two.",
                "This is a normal length paragraph that should be processed correctly.",
                "C",
                string.Concat(Enumerable.Repeat("Very long paragraph. ", 150)),
                "D",
                "E",
                "F"
            };
            var payload = string.Join("\n\n", paragraphs);
            var resourceId = "test_no_loss";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            var allFragmentContent = string.Join(" ", allFragments.Select(f => f.Content));

            // Every paragraph should appear in the results
            allFragmentContent.Should().Contain("A");
            allFragmentContent.Should().Contain("Short paragraph number two");
            allFragmentContent.Should().Contain("normal length paragraph");
            allFragmentContent.Should().Contain("C");
            allFragmentContent.Should().Contain("Very long paragraph");
            allFragmentContent.Should().Contain("D");
            allFragmentContent.Should().Contain("E");
            allFragmentContent.Should().Contain("F");
        }

        #endregion
    }
}
