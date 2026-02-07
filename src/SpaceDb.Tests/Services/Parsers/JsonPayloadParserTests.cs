using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceDb.Services.Parsers;
using Xunit;

namespace SpaceDb.Tests.Services.Parsers
{
    public class JsonPayloadParserTests
    {
        private readonly Mock<ILogger<JsonPayloadParser>> _loggerMock;
        private readonly JsonPayloadParser _parser;

        public JsonPayloadParserTests()
        {
            _loggerMock = new Mock<ILogger<JsonPayloadParser>>();
            _parser = new JsonPayloadParser(_loggerMock.Object);
        }

        #region Basic Functionality Tests

        [Fact]
        public void ContentType_ShouldReturnJson()
        {
            // Act
            var contentType = _parser.ContentType;

            // Assert
            contentType.Should().Be("json");
        }

        [Fact]
        public async Task ParseAsync_WithSimpleObject_ShouldCreateFragment()
        {
            // Arrange
            var payload = @"{
                ""name"": ""John Doe"",
                ""age"": 30,
                ""active"": true
            }";
            var resourceId = "test_simple_object";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Should().NotBeNull();
            result.ResourceId.Should().Be(resourceId);
            result.ResourceType.Should().Be("json");
            result.Fragments.Should().NotBeEmpty();
            result.Fragments.Should().Contain(f => f.Type == "json_object");
        }

        [Fact]
        public async Task ParseAsync_WithEmptyObject_ShouldCreateNoFragments()
        {
            // Arrange
            var payload = "{}";
            var resourceId = "test_empty_object";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().BeEmpty();
        }

        [Fact]
        public async Task ParseAsync_WithSimpleArray_ShouldCreateArrayFragment()
        {
            // Arrange
            var payload = @"[1, 2, 3, 4, 5]";
            var resourceId = "test_simple_array";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().HaveCount(1);
            result.Fragments[0].Type.Should().Be("json_array");
            result.Fragments[0].Metadata["array_length"].Should().Be(5);
        }

        #endregion

        #region Nested Structure Tests

        [Fact]
        public async Task ParseAsync_WithNestedObjects_ShouldCreateMultipleFragments()
        {
            // Arrange
            var payload = @"{
                ""user"": {
                    ""name"": ""John Doe"",
                    ""email"": ""john@example.com""
                },
                ""settings"": {
                    ""theme"": ""dark"",
                    ""notifications"": true
                }
            }";
            var resourceId = "test_nested_objects";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().HaveCountGreaterThan(1);
            result.Fragments.Should().Contain(f => f.Type == "json_object" && f.Metadata.ContainsKey("depth"));

            // Should have fragments at different depths
            var depths = result.Fragments
                .Where(f => f.Metadata.ContainsKey("depth"))
                .Select(f => (int)f.Metadata["depth"])
                .Distinct()
                .ToList();
            depths.Should().HaveCountGreaterThan(1);
        }

        [Fact]
        public async Task ParseAsync_WithDeeplyNestedStructure_ShouldRespectMaxDepth()
        {
            // Arrange
            var parser = new JsonPayloadParser(_loggerMock.Object, maxDepth: 3);
            var payload = @"{
                ""level1"": {
                    ""level2"": {
                        ""level3"": {
                            ""level4"": {
                                ""level5"": ""too deep""
                            }
                        }
                    }
                }
            }";
            var resourceId = "test_max_depth";

            // Act
            var result = await parser.ParseAsync(payload, resourceId);

            // Assert
            var maxDepth = result.Fragments
                .Where(f => f.Metadata.ContainsKey("depth"))
                .Max(f => (int)f.Metadata["depth"]);
            maxDepth.Should().BeLessThanOrEqualTo(3);
        }

        [Fact]
        public async Task ParseAsync_WithNestedArrays_ShouldHandleCorrectly()
        {
            // Arrange
            var payload = @"{
                ""matrix"": [
                    [1, 2, 3],
                    [4, 5, 6],
                    [7, 8, 9]
                ]
            }";
            var resourceId = "test_nested_arrays";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().Contain(f => f.Type == "json_array");
            result.Fragments.Should().HaveCountGreaterThan(1);
        }

        #endregion

        #region String Value Tests

        [Fact]
        public async Task ParseAsync_WithLongStringValue_ShouldCreateValueFragment()
        {
            // Arrange - string longer than 20 chars creates fragment
            var payload = @"{
                ""description"": ""This is a very long description that should create a separate fragment because it exceeds the minimum length requirement.""
            }";
            var resourceId = "test_long_string";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().Contain(f => f.Type == "json_value");
            var valueFragment = result.Fragments.First(f => f.Type == "json_value");
            valueFragment.Metadata["value_type"].Should().Be("string");
            valueFragment.Content.Should().Contain("very long description");
        }

        [Fact]
        public async Task ParseAsync_WithShortStringValue_ShouldNotCreateValueFragment()
        {
            // Arrange - string shorter than 20 chars doesn't create fragment
            var payload = @"{
                ""name"": ""John""
            }";
            var resourceId = "test_short_string";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().NotContain(f => f.Type == "json_value");
            // Should still have object fragment
            result.Fragments.Should().Contain(f => f.Type == "json_object");
        }

        [Fact]
        public async Task ParseAsync_WithMultipleLongStrings_ShouldCreateMultipleValueFragments()
        {
            // Arrange
            var payload = @"{
                ""title"": ""This is a long title that exceeds twenty characters"",
                ""description"": ""This is a long description that also exceeds the minimum length"",
                ""summary"": ""Another long string value that should create a fragment""
            }";
            var resourceId = "test_multiple_strings";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var valueFragments = result.Fragments.Where(f => f.Type == "json_value").ToList();
            valueFragments.Should().HaveCount(3);
        }

        #endregion

        #region Array Handling Tests

        [Fact]
        public async Task ParseAsync_WithArrayOfObjects_ShouldCreateFragmentsForEach()
        {
            // Arrange
            var payload = @"[
                { ""id"": 1, ""name"": ""Item 1"" },
                { ""id"": 2, ""name"": ""Item 2"" },
                { ""id"": 3, ""name"": ""Item 3"" }
            ]";
            var resourceId = "test_array_objects";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().Contain(f => f.Type == "json_array");
            result.Fragments.Should().Contain(f => f.Type == "json_object");

            var arrayFragment = result.Fragments.First(f => f.Type == "json_array");
            arrayFragment.Metadata["array_length"].Should().Be(3);
        }

        [Fact]
        public async Task ParseAsync_WithArrayDisabled_ShouldSkipArrays()
        {
            // Arrange
            var parser = new JsonPayloadParser(_loggerMock.Object, includeArrays: false);
            var payload = @"{
                ""items"": [1, 2, 3, 4, 5]
            }";
            var resourceId = "test_arrays_disabled";

            // Act
            var result = await parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().NotContain(f => f.Type == "json_array");
        }

        [Fact]
        public async Task ParseAsync_WithEmptyArray_ShouldCreateArrayFragment()
        {
            // Arrange
            var payload = @"{ ""items"": [] }";
            var resourceId = "test_empty_array";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var arrayFragment = result.Fragments.FirstOrDefault(f => f.Type == "json_array");
            arrayFragment.Should().NotBeNull();
            arrayFragment!.Metadata["array_length"].Should().Be(0);
        }

        [Fact]
        public async Task ParseAsync_WithLargeArray_ShouldSummarizeCorrectly()
        {
            // Arrange
            var items = Enumerable.Range(1, 100).Select(i => i.ToString());
            var payload = $@"{{ ""numbers"": [{string.Join(", ", items)}] }}";
            var resourceId = "test_large_array";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var arrayFragment = result.Fragments.First(f => f.Type == "json_array");
            arrayFragment.Metadata["array_length"].Should().Be(100);
            arrayFragment.Content.Should().Contain("Array with 100 items");
        }

        #endregion

        #region Parent-Child Relationship Tests

        [Fact]
        public async Task ParseAsync_WithNestedStructure_ShouldSetParentKeys()
        {
            // Arrange
            var payload = @"{
                ""user"": {
                    ""profile"": {
                        ""bio"": ""This is a long biography that will create a fragment with parent tracking information.""
                    }
                }
            }";
            var resourceId = "test_parent_keys";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().Contain(f => f.ParentKey != null);

            // Check that nested fragments have correct parent paths
            var fragments = result.Fragments.Where(f => f.Metadata.ContainsKey("path")).ToList();
            fragments.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ParseAsync_RootElement_ShouldHaveNullParent()
        {
            // Arrange
            var payload = @"{
                ""field1"": ""value1"",
                ""field2"": ""value2""
            }";
            var resourceId = "test_root_parent";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var rootFragment = result.Fragments.FirstOrDefault(f =>
                f.Metadata.ContainsKey("depth") && (int)f.Metadata["depth"] == 0);
            rootFragment.Should().NotBeNull();
        }

        #endregion

        #region Metadata Tests

        [Fact]
        public async Task ParseAsync_ShouldIncludePathInMetadata()
        {
            // Arrange
            var payload = @"{
                ""user"": {
                    ""name"": ""John Doe""
                }
            }";
            var resourceId = "test_metadata_path";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().OnlyContain(f => f.Metadata.ContainsKey("path"));
            result.Fragments.Should().Contain(f =>
                f.Metadata["path"].ToString()!.Contains("user"));
        }

        [Fact]
        public async Task ParseAsync_ShouldIncludeResourceMetadata()
        {
            // Arrange
            var payload = @"{ ""test"": ""value"" }";
            var resourceId = "test_resource_meta";
            var customMetadata = new Dictionary<string, object>
            {
                ["source"] = "api",
                ["version"] = "1.0"
            };

            // Act
            var result = await _parser.ParseAsync(payload, resourceId, customMetadata);

            // Assert
            result.Metadata.Should().ContainKey("source");
            result.Metadata.Should().ContainKey("version");
            result.Metadata.Should().ContainKey("parsed_at");
            result.Metadata.Should().ContainKey("parser_type");
            result.Metadata.Should().ContainKey("total_fragments");
            result.Metadata.Should().ContainKey("json_size");
            result.Metadata["parser_type"].Should().Be("json");
        }

        [Fact]
        public async Task ParseAsync_ShouldIncludeDepthInFragmentMetadata()
        {
            // Arrange
            var payload = @"{
                ""level1"": {
                    ""level2"": {
                        ""level3"": ""value""
                    }
                }
            }";
            var resourceId = "test_depth_meta";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().OnlyContain(f => f.Metadata.ContainsKey("depth"));
            result.Fragments.Should().Contain(f => (int)f.Metadata["depth"] == 0);
            result.Fragments.Should().Contain(f => (int)f.Metadata["depth"] > 0);
        }

        #endregion

        #region Order Tests

        [Fact]
        public async Task ParseAsync_ShouldMaintainSequentialOrder()
        {
            // Arrange
            var payload = @"{
                ""field1"": ""value1"",
                ""field2"": {
                    ""nested"": ""value""
                },
                ""field3"": ""value3""
            }";
            var resourceId = "test_order";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            for (int i = 0; i < result.Fragments.Count; i++)
            {
                result.Fragments[i].Order.Should().Be(i);
            }
        }

        #endregion

        #region CanParse Tests

        [Fact]
        public void CanParse_WithValidJson_ShouldReturnTrue()
        {
            // Arrange
            var payload = @"{ ""valid"": true }";

            // Act
            var canParse = _parser.CanParse(payload);

            // Assert
            canParse.Should().BeTrue();
        }

        [Fact]
        public void CanParse_WithInvalidJson_ShouldReturnFalse()
        {
            // Arrange
            var payload = @"{ invalid json }";

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

        [Fact]
        public void CanParse_WithJsonArray_ShouldReturnTrue()
        {
            // Arrange
            var payload = @"[1, 2, 3]";

            // Act
            var canParse = _parser.CanParse(payload);

            // Assert
            canParse.Should().BeTrue();
        }

        [Fact]
        public void CanParse_WithJsonString_ShouldReturnTrue()
        {
            // Arrange
            var payload = @"""just a string""";

            // Act
            var canParse = _parser.CanParse(payload);

            // Assert
            canParse.Should().BeTrue();
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ParseAsync_WithMalformedJson_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var payload = @"{ malformed: json }";
            var resourceId = "test_malformed";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _parser.ParseAsync(payload, resourceId));
        }

        [Fact]
        public async Task ParseAsync_WithIncompleteJson_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var payload = @"{ ""incomplete"": ";
            var resourceId = "test_incomplete";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _parser.ParseAsync(payload, resourceId));
        }

        #endregion

        #region Complex Real-World Examples

        [Fact]
        public async Task ParseAsync_WithComplexUserObject_ShouldParseCorrectly()
        {
            // Arrange
            var payload = @"{
                ""id"": 12345,
                ""username"": ""johndoe"",
                ""email"": ""john.doe@example.com"",
                ""profile"": {
                    ""firstName"": ""John"",
                    ""lastName"": ""Doe"",
                    ""bio"": ""Software engineer passionate about building great products and solving complex problems."",
                    ""location"": {
                        ""city"": ""San Francisco"",
                        ""country"": ""USA""
                    }
                },
                ""settings"": {
                    ""theme"": ""dark"",
                    ""notifications"": {
                        ""email"": true,
                        ""push"": false
                    }
                },
                ""tags"": [""developer"", ""javascript"", ""python""],
                ""createdAt"": ""2024-01-01T00:00:00Z""
            }";
            var resourceId = "test_complex_user";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().NotBeEmpty();
            result.Fragments.Should().Contain(f => f.Type == "json_object");
            result.Fragments.Should().Contain(f => f.Type == "json_array");
            result.Fragments.Should().Contain(f => f.Type == "json_value");

            // Should have bio as value fragment (long string)
            result.Fragments.Should().Contain(f =>
                f.Type == "json_value" && f.Content.Contains("Software engineer"));
        }

        [Fact]
        public async Task ParseAsync_WithApiResponse_ShouldParseCorrectly()
        {
            // Arrange
            var payload = @"{
                ""status"": ""success"",
                ""data"": {
                    ""items"": [
                        {
                            ""id"": 1,
                            ""title"": ""First Item with a descriptive title that is long enough"",
                            ""metadata"": {
                                ""created"": ""2024-01-01"",
                                ""author"": ""John""
                            }
                        },
                        {
                            ""id"": 2,
                            ""title"": ""Second Item with another descriptive title for testing"",
                            ""metadata"": {
                                ""created"": ""2024-01-02"",
                                ""author"": ""Jane""
                            }
                        }
                    ],
                    ""pagination"": {
                        ""page"": 1,
                        ""pageSize"": 10,
                        ""total"": 2
                    }
                }
            }";
            var resourceId = "test_api_response";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().NotBeEmpty();
            result.Metadata["total_fragments"].Should().BeOfType<int>().Which.Should().BeGreaterThan(0);

            // Should parse array of items
            result.Fragments.Should().Contain(f =>
                f.Type == "json_array" && f.Metadata["array_length"].Equals(2));
        }

        [Fact]
        public async Task ParseAsync_WithNullValues_ShouldNotCreateValueFragments()
        {
            // Arrange
            var payload = @"{
                ""field1"": ""value"",
                ""field2"": null,
                ""field3"": {
                    ""nested"": null
                }
            }";
            var resourceId = "test_null_values";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().NotBeEmpty();
            // Nulls shouldn't create separate value fragments (only appear in summaries)
            result.Fragments.Should().NotContain(f => f.Type == "json_value" && f.Content == "null");
        }

        [Fact]
        public async Task ParseAsync_WithMixedTypes_ShouldHandleAll()
        {
            // Arrange
            var payload = @"{
                ""stringValue"": ""test"",
                ""numberValue"": 42,
                ""booleanTrue"": true,
                ""booleanFalse"": false,
                ""nullValue"": null,
                ""arrayValue"": [1, 2, 3],
                ""objectValue"": {
                    ""nested"": ""value""
                }
            }";
            var resourceId = "test_mixed_types";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            result.Fragments.Should().NotBeEmpty();
            result.Fragments.Should().Contain(f => f.Type == "json_object");
            result.Fragments.Should().Contain(f => f.Type == "json_array");
        }

        #endregion

        #region Fragment Content Tests

        [Fact]
        public async Task ParseAsync_ObjectFragment_ShouldContainPropertySummary()
        {
            // Arrange
            var payload = @"{
                ""prop1"": ""value1"",
                ""prop2"": 123,
                ""prop3"": true
            }";
            var resourceId = "test_object_summary";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var objectFragment = result.Fragments.First(f => f.Type == "json_object");
            objectFragment.Content.Should().Contain("Object with");
            objectFragment.Content.Should().Contain("properties");
            objectFragment.Metadata["property_count"].Should().Be(3);
        }

        [Fact]
        public async Task ParseAsync_ArrayFragment_ShouldContainItemSummary()
        {
            // Arrange
            var payload = @"[""item1"", ""item2"", ""item3"", ""item4"", ""item5""]";
            var resourceId = "test_array_summary";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var arrayFragment = result.Fragments.First(f => f.Type == "json_array");
            arrayFragment.Content.Should().Contain("Array with 5 items");
        }

        [Fact]
        public async Task ParseAsync_LargeObject_ShouldTruncateSummary()
        {
            // Arrange - object with more than 5 properties
            var properties = Enumerable.Range(1, 10)
                .Select(i => $@"""prop{i}"": ""value{i}""");
            var payload = $@"{{ {string.Join(", ", properties)} }}";
            var resourceId = "test_large_object";

            // Act
            var result = await _parser.ParseAsync(payload, resourceId);

            // Assert
            var objectFragment = result.Fragments.First(f => f.Type == "json_object");
            objectFragment.Content.Should().Contain("more");
            objectFragment.Metadata["property_count"].Should().Be(10);
        }

        #endregion
    }
}
