using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceDb.Services.Parsers;
using Xunit;

namespace SpaceDb.Tests.Services.Parsers
{
    /// <summary>
    /// Tests for block creation functionality in parsers
    /// Ensures fragments are properly grouped into blocks based on size limits
    /// </summary>
    public class BlockCreationTests
    {
        private readonly Mock<ILogger<TextPayloadParser>> _loggerMock;

        public BlockCreationTests()
        {
            _loggerMock = new Mock<ILogger<TextPayloadParser>>();
        }

        [Fact]
        public async Task ParseAsync_WithSmallContent_ShouldCreateSingleBlock()
        {
            // Arrange
            var parser = new TextPayloadParser(_loggerMock.Object);
            var payload = "This is a small paragraph that should fit in one block easily.";
            var resourceId = "test_single_block";

            // Act
            var result = await parser.ParseAsync(payload, resourceId);

            // Assert
            result.Blocks.Should().HaveCount(1);
            result.Blocks[0].Fragments.Should().HaveCount(1);
            result.Blocks[0].Order.Should().Be(0);
        }

        [Fact]
        public async Task ParseAsync_WithContentExceedingBlockSize_ShouldCreateMultipleBlocks()
        {
            // Arrange - use small block size to test splitting
            var parser = new TextPayloadParser(_loggerMock.Object, maxBlockSize: 500);

            // Create content with multiple paragraphs that will exceed block size
            var paragraphs = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                paragraphs.Add($"Paragraph {i} with sufficient content to meet minimum length requirements. " +
                              $"This paragraph contains enough text to be considered valid by the parser.");
            }
            var payload = string.Join("\n\n", paragraphs);
            var resourceId = "test_multiple_blocks";

            // Act
            var result = await parser.ParseAsync(payload, resourceId);

            // Assert
            result.Blocks.Should().HaveCountGreaterThan(1);

            // Verify each block respects size limit (allowing some margin for joining)
            foreach (var block in result.Blocks)
            {
                block.Content.Length.Should().BeLessThanOrEqualTo(550); // 10% margin
                block.Fragments.Should().NotBeEmpty();
            }
        }

        [Fact]
        public async Task ParseAsync_BlockContent_ShouldBeJoinedFragments()
        {
            // Arrange
            var parser = new TextPayloadParser(_loggerMock.Object);
            var payload = @"First paragraph with enough content to be valid.

Second paragraph also with sufficient text content.

Third paragraph completing the test scenario.";
            var resourceId = "test_block_content";

            // Act
            var result = await parser.ParseAsync(payload, resourceId);

            // Assert
            result.Blocks.Should().HaveCount(1);
            var block = result.Blocks[0];

            // Block content should contain all fragments
            block.Content.Should().Contain("First paragraph");
            block.Content.Should().Contain("Second paragraph");
            block.Content.Should().Contain("Third paragraph");

            // Verify fragments are preserved
            block.Fragments.Should().HaveCount(3);
        }

        [Fact]
        public async Task ParseAsync_BlockMetadata_ShouldIncludeFragmentCountAndSize()
        {
            // Arrange
            var parser = new TextPayloadParser(_loggerMock.Object);
            var payload = "Test paragraph with adequate length for processing and verification.";
            var resourceId = "test_block_metadata";

            // Act
            var result = await parser.ParseAsync(payload, resourceId);

            // Assert
            var block = result.Blocks[0];
            block.Metadata.Should().ContainKey("fragment_count");
            block.Metadata.Should().ContainKey("size");
            block.Metadata["fragment_count"].Should().Be(1);
            ((int)block.Metadata["size"]).Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ParseAsync_BlocksOrder_ShouldBeSequential()
        {
            // Arrange
            var parser = new TextPayloadParser(_loggerMock.Object, maxBlockSize: 300);

            var paragraphs = new List<string>();
            for (int i = 0; i < 15; i++)
            {
                paragraphs.Add($"Paragraph number {i} with adequate content for the test purpose and validation.");
            }
            var payload = string.Join("\n\n", paragraphs);
            var resourceId = "test_block_order";

            // Act
            var result = await parser.ParseAsync(payload, resourceId);

            // Assert
            result.Blocks.Should().HaveCountGreaterThan(1);

            for (int i = 0; i < result.Blocks.Count; i++)
            {
                result.Blocks[i].Order.Should().Be(i);
            }
        }

        [Fact]
        public async Task ParseAsync_FragmentsSplitAcrossBlocks_ShouldMaintainOrderGlobally()
        {
            // Arrange
            var parser = new TextPayloadParser(_loggerMock.Object, maxBlockSize: 200);

            var payload = @"Alpha paragraph with sufficient length content.

Beta paragraph with sufficient length content.

Gamma paragraph with sufficient length content.

Delta paragraph with sufficient length content.";
            var resourceId = "test_fragment_order_across_blocks";

            // Act
            var result = await parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            var allContent = string.Join(" ", allFragments.Select(f => f.Content));

            // Verify all fragments are present
            allContent.Should().Contain("Alpha");
            allContent.Should().Contain("Beta");
            allContent.Should().Contain("Gamma");
            allContent.Should().Contain("Delta");

            // Verify fragments maintain sequential ordering
            for (int i = 0; i < allFragments.Count; i++)
            {
                allFragments[i].Order.Should().Be(i);
            }
        }

        [Fact]
        public async Task ParseAsync_NoFragmentsShouldBeLost_WhenSplitIntoBlocks()
        {
            // Arrange
            var parser = new TextPayloadParser(_loggerMock.Object, maxBlockSize: 400);

            var expectedContent = new List<string>
            {
                "First unique marker paragraph with adequate length.",
                "Second unique marker paragraph with adequate length.",
                "Third unique marker paragraph with adequate length.",
                "Fourth unique marker paragraph with adequate length.",
                "Fifth unique marker paragraph with adequate length.",
                "Sixth unique marker paragraph with adequate length.",
                "Seventh unique marker paragraph with adequate length.",
                "Eighth unique marker paragraph with adequate length."
            };
            var payload = string.Join("\n\n", expectedContent);
            var resourceId = "test_no_loss";

            // Act
            var result = await parser.ParseAsync(payload, resourceId);

            // Assert
            var allFragments = result.Blocks.SelectMany(b => b.Fragments).ToList();
            var allContent = string.Join(" ", allFragments.Select(f => f.Content));

            // Verify all content is preserved
            allContent.Should().Contain("First unique marker");
            allContent.Should().Contain("Second unique marker");
            allContent.Should().Contain("Third unique marker");
            allContent.Should().Contain("Fourth unique marker");
            allContent.Should().Contain("Fifth unique marker");
            allContent.Should().Contain("Sixth unique marker");
            allContent.Should().Contain("Seventh unique marker");
            allContent.Should().Contain("Eighth unique marker");
        }

        [Fact]
        public async Task ParseAsync_ResourceMetadata_ShouldIncludeTotalBlocks()
        {
            // Arrange
            var parser = new TextPayloadParser(_loggerMock.Object, maxBlockSize: 300);

            var paragraphs = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                paragraphs.Add($"Paragraph {i} with adequate length and content for testing purposes.");
            }
            var payload = string.Join("\n\n", paragraphs);
            var resourceId = "test_metadata";

            // Act
            var result = await parser.ParseAsync(payload, resourceId);

            // Assert
            result.Metadata.Should().ContainKey("total_blocks");
            result.Metadata.Should().ContainKey("total_fragments");
            ((int)result.Metadata["total_blocks"]).Should().Be(result.Blocks.Count);
            ((int)result.Metadata["total_fragments"]).Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ParseAsync_JsonParser_ShouldAlsoCreateBlocks()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<JsonPayloadParser>>();
            var parser = new JsonPayloadParser(loggerMock.Object, maxBlockSize: 200);

            var payload = @"{
                ""field1"": ""This is a longer string value that should be included in fragments"",
                ""field2"": ""Another longer string value for testing block creation"",
                ""field3"": ""Yet another string with sufficient length for fragment creation"",
                ""field4"": ""One more string to ensure we have enough content""
            }";
            var resourceId = "test_json_blocks";

            // Act
            var result = await parser.ParseAsync(payload, resourceId);

            // Assert
            result.Blocks.Should().NotBeEmpty();
            result.Blocks.All(b => b.Fragments.Count > 0).Should().BeTrue();
            result.Metadata.Should().ContainKey("total_blocks");
        }
    }
}
