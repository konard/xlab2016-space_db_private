using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceDb.Models;
using SpaceDb.Services.Parsers;
using Xunit;

namespace SpaceDb.Tests.Services.Parsers
{
    public class PayloadParserBaseTests
    {
        private readonly Mock<ILogger<TestPayloadParser>> _loggerMock;

        public PayloadParserBaseTests()
        {
            _loggerMock = new Mock<ILogger<TestPayloadParser>>();
        }

        #region Test Implementation

        public class TestPayloadParser : PayloadParserBase
        {
            private readonly Func<string, string, Dictionary<string, object>?, Task<ParsedResource>>? _parseFunc;

            public TestPayloadParser(ILogger logger) : base(logger)
            {
            }

            public TestPayloadParser(
                ILogger logger,
                Func<string, string, Dictionary<string, object>?, Task<ParsedResource>> parseFunc)
                : base(logger)
            {
                _parseFunc = parseFunc;
            }

            public override string ContentType => "test";

            public override Task<ParsedResource> ParseAsync(
                string payload,
                string resourceId,
                Dictionary<string, object>? metadata = null)
            {
                if (_parseFunc != null)
                {
                    return _parseFunc(payload, resourceId, metadata);
                }

                var result = new ParsedResource
                {
                    ResourceId = resourceId,
                    ResourceType = ContentType,
                    Metadata = CreateMetadata(metadata)
                };

                var fragment = new ContentFragment
                {
                    Content = payload,
                    Type = "test_fragment",
                    Order = 0
                };

                result.Blocks.Add(new ContentBlock
                {
                    Content = payload,
                    Type = "test_block",
                    Order = 0,
                    Fragments = new List<ContentFragment> { fragment }
                });

                return Task.FromResult(result);
            }

            // Expose protected methods for testing
            public string TestNormalizeText(string text) => NormalizeText(text);
            public Dictionary<string, object> TestCreateMetadata(Dictionary<string, object>? baseMetadata = null)
                => CreateMetadata(baseMetadata);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestPayloadParser(null!));
        }

        [Fact]
        public void Constructor_WithValidLogger_ShouldSucceed()
        {
            // Act
            var parser = new TestPayloadParser(_loggerMock.Object);

            // Assert
            parser.Should().NotBeNull();
            parser.ContentType.Should().Be("test");
        }

        #endregion

        #region CanParse Tests

        [Fact]
        public void CanParse_WithValidText_ShouldReturnTrue()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var payload = "Valid payload content";

            // Act
            var canParse = parser.CanParse(payload);

            // Assert
            canParse.Should().BeTrue();
        }

        [Fact]
        public void CanParse_WithEmptyString_ShouldReturnFalse()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var payload = "";

            // Act
            var canParse = parser.CanParse(payload);

            // Assert
            canParse.Should().BeFalse();
        }

        [Fact]
        public void CanParse_WithWhitespaceOnly_ShouldReturnFalse()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var payload = "   \t\n\r   ";

            // Act
            var canParse = parser.CanParse(payload);

            // Assert
            canParse.Should().BeFalse();
        }

        [Fact]
        public void CanParse_WithNull_ShouldReturnFalse()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            string? payload = null;

            // Act
            var canParse = parser.CanParse(payload!);

            // Assert
            canParse.Should().BeFalse();
        }

        #endregion

        #region NormalizeText Tests

        [Fact]
        public void NormalizeText_WithNormalText_ShouldTrimAndClean()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var text = "  Normal text with spaces  ";

            // Act
            var normalized = parser.TestNormalizeText(text);

            // Assert
            normalized.Should().Be("Normal text with spaces");
        }

        [Fact]
        public void NormalizeText_WithExcessiveWhitespace_ShouldCollapseToSingle()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var text = "Text    with     excessive     whitespace";

            // Act
            var normalized = parser.TestNormalizeText(text);

            // Assert
            normalized.Should().Be("Text with excessive whitespace");
            normalized.Should().NotContain("  ");
        }

        [Fact]
        public void NormalizeText_WithMultipleLineBreaks_ShouldCollapseToSingle()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var text = "Line 1\n\n\nLine 2\r\n\r\n\r\nLine 3";

            // Act
            var normalized = parser.TestNormalizeText(text);

            // Assert
            normalized.Should().NotContain("\n\n");
            normalized.Should().NotContain("\r\n\r\n");
        }

        [Fact]
        public void NormalizeText_WithTabsAndNewlines_ShouldReplaceWithSpaces()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var text = "Text\twith\ttabs\nand\nnewlines";

            // Act
            var normalized = parser.TestNormalizeText(text);

            // Assert
            normalized.Should().Be("Text with tabs and newlines");
        }

        [Fact]
        public void NormalizeText_WithEmptyString_ShouldReturnEmpty()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var text = "";

            // Act
            var normalized = parser.TestNormalizeText(text);

            // Assert
            normalized.Should().BeEmpty();
        }

        [Fact]
        public void NormalizeText_WithWhitespaceOnly_ShouldReturnEmpty()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var text = "   \n\t\r   ";

            // Act
            var normalized = parser.TestNormalizeText(text);

            // Assert
            normalized.Should().BeEmpty();
        }

        [Fact]
        public void NormalizeText_WithNull_ShouldReturnEmpty()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            string? text = null;

            // Act
            var normalized = parser.TestNormalizeText(text!);

            // Assert
            normalized.Should().BeEmpty();
        }

        [Fact]
        public void NormalizeText_WithLeadingAndTrailingWhitespace_ShouldTrim()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var text = "\n\t  Leading and trailing whitespace  \t\n";

            // Act
            var normalized = parser.TestNormalizeText(text);

            // Assert
            normalized.Should().Be("Leading and trailing whitespace");
        }

        #endregion

        #region CreateMetadata Tests

        [Fact]
        public void CreateMetadata_WithNoBaseMetadata_ShouldCreateDefaultFields()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);

            // Act
            var metadata = parser.TestCreateMetadata();

            // Assert
            metadata.Should().ContainKey("parsed_at");
            metadata.Should().ContainKey("parser_type");
            metadata["parser_type"].Should().Be("test");
            metadata["parsed_at"].Should().BeOfType<DateTime>();
        }

        [Fact]
        public void CreateMetadata_WithBaseMetadata_ShouldMergeFields()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var baseMetadata = new Dictionary<string, object>
            {
                ["custom_field"] = "custom_value",
                ["author"] = "test_author"
            };

            // Act
            var metadata = parser.TestCreateMetadata(baseMetadata);

            // Assert
            metadata.Should().ContainKey("custom_field");
            metadata.Should().ContainKey("author");
            metadata.Should().ContainKey("parsed_at");
            metadata.Should().ContainKey("parser_type");
            metadata["custom_field"].Should().Be("custom_value");
            metadata["author"].Should().Be("test_author");
        }

        [Fact]
        public void CreateMetadata_WithBaseMetadata_ShouldNotModifyOriginal()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var baseMetadata = new Dictionary<string, object>
            {
                ["field1"] = "value1"
            };
            var originalCount = baseMetadata.Count;

            // Act
            var metadata = parser.TestCreateMetadata(baseMetadata);

            // Assert
            baseMetadata.Count.Should().Be(originalCount);
            metadata.Count.Should().BeGreaterThan(originalCount);
        }

        [Fact]
        public void CreateMetadata_ParsedAt_ShouldBeRecentTime()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var before = DateTime.UtcNow;

            // Act
            var metadata = parser.TestCreateMetadata();
            var after = DateTime.UtcNow;

            // Assert
            var parsedAt = (DateTime)metadata["parsed_at"];
            parsedAt.Should().BeOnOrAfter(before);
            parsedAt.Should().BeOnOrBefore(after);
        }

        [Fact]
        public void CreateMetadata_WithNullBaseMetadata_ShouldCreateNewDictionary()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);

            // Act
            var metadata = parser.TestCreateMetadata(null);

            // Assert
            metadata.Should().NotBeNull();
            metadata.Should().ContainKey("parsed_at");
            metadata.Should().ContainKey("parser_type");
        }

        #endregion

        #region ParseAsync Abstract Implementation Tests

        [Fact]
        public async Task ParseAsync_ShouldCallImplementation()
        {
            // Arrange
            var callCount = 0;
            var parser = new TestPayloadParser(
                _loggerMock.Object,
                (payload, resourceId, metadata) =>
                {
                    callCount++;
                    return Task.FromResult(new ParsedResource
                    {
                        ResourceId = resourceId,
                        ResourceType = "test",
                        Metadata = new Dictionary<string, object>()
                    });
                });

            // Act
            await parser.ParseAsync("test payload", "test_resource");

            // Assert
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task ParseAsync_ShouldPassPayloadToImplementation()
        {
            // Arrange
            string? receivedPayload = null;
            var parser = new TestPayloadParser(
                _loggerMock.Object,
                (payload, resourceId, metadata) =>
                {
                    receivedPayload = payload;
                    return Task.FromResult(new ParsedResource
                    {
                        ResourceId = resourceId,
                        ResourceType = "test",
                        Metadata = new Dictionary<string, object>()
                    });
                });

            // Act
            await parser.ParseAsync("expected payload", "test_resource");

            // Assert
            receivedPayload.Should().Be("expected payload");
        }

        [Fact]
        public async Task ParseAsync_ShouldPassResourceIdToImplementation()
        {
            // Arrange
            string? receivedResourceId = null;
            var parser = new TestPayloadParser(
                _loggerMock.Object,
                (payload, resourceId, metadata) =>
                {
                    receivedResourceId = resourceId;
                    return Task.FromResult(new ParsedResource
                    {
                        ResourceId = resourceId,
                        ResourceType = "test",
                        Metadata = new Dictionary<string, object>()
                    });
                });

            // Act
            await parser.ParseAsync("payload", "expected_resource_id");

            // Assert
            receivedResourceId.Should().Be("expected_resource_id");
        }

        [Fact]
        public async Task ParseAsync_ShouldPassMetadataToImplementation()
        {
            // Arrange
            Dictionary<string, object>? receivedMetadata = null;
            var parser = new TestPayloadParser(
                _loggerMock.Object,
                (payload, resourceId, metadata) =>
                {
                    receivedMetadata = metadata;
                    return Task.FromResult(new ParsedResource
                    {
                        ResourceId = resourceId,
                        ResourceType = "test",
                        Metadata = new Dictionary<string, object>()
                    });
                });
            var expectedMetadata = new Dictionary<string, object>
            {
                ["key"] = "value"
            };

            // Act
            await parser.ParseAsync("payload", "resource", expectedMetadata);

            // Assert
            receivedMetadata.Should().BeSameAs(expectedMetadata);
        }

        [Fact]
        public async Task ParseAsync_WithDefaultImplementation_ShouldCreateResult()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);

            // Act
            var result = await parser.ParseAsync("test payload", "test_resource");

            // Assert
            result.Should().NotBeNull();
            result.ResourceId.Should().Be("test_resource");
            result.ResourceType.Should().Be("test");
            result.Fragments.Should().HaveCount(1);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task ParseAsync_ShouldUseCreateMetadataHelper()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var customMetadata = new Dictionary<string, object>
            {
                ["source"] = "integration_test"
            };

            // Act
            var result = await parser.ParseAsync("payload", "resource", customMetadata);

            // Assert
            result.Metadata.Should().ContainKey("source");
            result.Metadata.Should().ContainKey("parsed_at");
            result.Metadata.Should().ContainKey("parser_type");
            result.Metadata["source"].Should().Be("integration_test");
        }

        [Fact]
        public async Task ParseAsync_MultipleCalls_ShouldHaveDifferentTimestamps()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);

            // Act
            var result1 = await parser.ParseAsync("payload1", "resource1");
            await Task.Delay(10); // Small delay to ensure different timestamps
            var result2 = await parser.ParseAsync("payload2", "resource2");

            // Assert
            var time1 = (DateTime)result1.Metadata["parsed_at"];
            var time2 = (DateTime)result2.Metadata["parsed_at"];
            time2.Should().BeOnOrAfter(time1);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void NormalizeText_WithUnicodeCharacters_ShouldPreserve()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var text = "  Unicode: 你好 Привет مرحبا  ";

            // Act
            var normalized = parser.TestNormalizeText(text);

            // Assert
            normalized.Should().Contain("你好");
            normalized.Should().Contain("Привет");
            normalized.Should().Contain("مرحبا");
        }

        [Fact]
        public void NormalizeText_WithSpecialCharacters_ShouldPreserve()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var text = "Special chars: @#$%^&*()_+-=[]{}|;:',.<>?/~`";

            // Act
            var normalized = parser.TestNormalizeText(text);

            // Assert
            normalized.Should().Contain("@#$%^&*()_+-=[]{}|;:',.<>?/~`");
        }

        [Fact]
        public void CreateMetadata_WithComplexObjects_ShouldStore()
        {
            // Arrange
            var parser = new TestPayloadParser(_loggerMock.Object);
            var complexObject = new { Name = "Test", Value = 123 };
            var baseMetadata = new Dictionary<string, object>
            {
                ["complex"] = complexObject,
                ["list"] = new List<int> { 1, 2, 3 }
            };

            // Act
            var metadata = parser.TestCreateMetadata(baseMetadata);

            // Assert
            metadata.Should().ContainKey("complex");
            metadata.Should().ContainKey("list");
            metadata["complex"].Should().BeSameAs(complexObject);
        }

        #endregion
    }
}
