using SpaceDb.Models;
using System.Text.Json;

namespace SpaceDb.Services.Parsers
{
    /// <summary>
    /// Parser for JSON content - converts to graph structure
    /// </summary>
    public class JsonPayloadParser : PayloadParserBase
    {
        private readonly int _maxDepth;
        private readonly bool _includeArrays;

        public override string ContentType => "json";

        public JsonPayloadParser(
            ILogger<JsonPayloadParser> logger,
            int maxDepth = 10,
            bool includeArrays = true,
            int maxBlockSize = 8000) : base(logger, maxBlockSize)
        {
            _maxDepth = maxDepth;
            _includeArrays = includeArrays;
        }

        public override async Task<ParsedResource> ParseAsync(
            string payload,
            string resourceId,
            Dictionary<string, object>? metadata = null)
        {
            _logger.LogInformation("Parsing JSON payload for resource {ResourceId}", resourceId);

            var result = new ParsedResource
            {
                ResourceId = resourceId,
                ResourceType = ContentType,
                Metadata = CreateMetadata(metadata)
            };

            try
            {
                using var document = JsonDocument.Parse(payload);
                var fragments = new List<ContentFragment>();
                int order = 0;

                // Parse JSON tree recursively
                ParseJsonElement(
                    document.RootElement,
                    rootPath: resourceId,
                    fragments: fragments,
                    order: ref order,
                    depth: 0,
                    parentKey: null);

                // Now group fragments into blocks
                result.Blocks = CreateBlocksFromFragments(fragments);

                result.Metadata["total_fragments"] = fragments.Count;
                result.Metadata["total_blocks"] = result.Blocks.Count;
                result.Metadata["json_size"] = payload.Length;

                _logger.LogInformation("Parsed {FragmentCount} JSON nodes into {BlockCount} blocks from resource {ResourceId}",
                    fragments.Count, result.Blocks.Count, resourceId);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON payload for resource {ResourceId}", resourceId);
                throw new InvalidOperationException($"Invalid JSON payload: {ex.Message}", ex);
            }

            return await Task.FromResult(result);
        }

        private void ParseJsonElement(
            JsonElement element,
            string rootPath,
            List<ContentFragment> fragments,
            ref int order,
            int depth,
            string? parentKey)
        {
            if (depth > _maxDepth)
            {
                _logger.LogWarning("Max depth {MaxDepth} reached at path {Path}", _maxDepth, rootPath);
                return;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    ParseJsonObject(element, rootPath, fragments, ref order, depth, parentKey);
                    break;

                case JsonValueKind.Array:
                    if (_includeArrays)
                    {
                        ParseJsonArray(element, rootPath, fragments, ref order, depth, parentKey);
                    }
                    break;

                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    ParseJsonValue(element, rootPath, fragments, ref order, parentKey);
                    break;

                case JsonValueKind.Null:
                    // Skip null values
                    break;
            }
        }

        private void ParseJsonObject(
            JsonElement element,
            string rootPath,
            List<ContentFragment> fragments,
            ref int order,
            int depth,
            string? parentKey)
        {
            var properties = new List<string>();

            foreach (var property in element.EnumerateObject())
            {
                var propertyPath = $"{rootPath}.{property.Name}";
                properties.Add($"{property.Name}: {GetValuePreview(property.Value)}");

                // Create fragment for this property
                if (ShouldCreateFragment(property.Value))
                {
                    ParseJsonElement(
                        property.Value,
                        propertyPath,
                        fragments,
                        ref order,
                        depth + 1,
                        rootPath);
                }
            }

            // Create fragment for the object itself
            if (properties.Count > 0)
            {
                var objectSummary = $"Object with {properties.Count} properties: " +
                    string.Join(", ", properties.Take(5));

                if (properties.Count > 5)
                {
                    objectSummary += $", ... ({properties.Count - 5} more)";
                }

                fragments.Add(new ContentFragment
                {
                    Content = objectSummary,
                    Type = "json_object",
                    Order = order++,
                    ParentKey = parentKey,
                    Metadata = new Dictionary<string, object>
                    {
                        ["path"] = rootPath,
                        ["property_count"] = properties.Count,
                        ["depth"] = depth
                    }
                });
            }
        }

        private void ParseJsonArray(
            JsonElement element,
            string rootPath,
            List<ContentFragment> fragments,
            ref int order,
            int depth,
            string? parentKey)
        {
            var arrayLength = element.GetArrayLength();
            var items = new List<string>();

            int index = 0;
            foreach (var item in element.EnumerateArray())
            {
                var itemPath = $"{rootPath}[{index}]";
                items.Add(GetValuePreview(item));

                if (ShouldCreateFragment(item))
                {
                    ParseJsonElement(
                        item,
                        itemPath,
                        fragments,
                        ref order,
                        depth + 1,
                        rootPath);
                }

                index++;
            }

            // Create fragment for array summary
            var arraySummary = $"Array with {arrayLength} items";
            if (items.Count > 0)
            {
                arraySummary += ": " + string.Join(", ", items.Take(3));
                if (items.Count > 3)
                {
                    arraySummary += $", ... ({items.Count - 3} more)";
                }
            }

            fragments.Add(new ContentFragment
            {
                Content = arraySummary,
                Type = "json_array",
                Order = order++,
                ParentKey = parentKey,
                Metadata = new Dictionary<string, object>
                {
                    ["path"] = rootPath,
                    ["array_length"] = arrayLength,
                    ["depth"] = depth
                }
            });
        }

        private void ParseJsonValue(
            JsonElement element,
            string rootPath,
            List<ContentFragment> fragments,
            ref int order,
            string? parentKey)
        {
            var value = GetValueString(element);

            // Only create fragments for string values with meaningful content
            if (element.ValueKind == JsonValueKind.String && value.Length > 20)
            {
                fragments.Add(new ContentFragment
                {
                    Content = value,
                    Type = "json_value",
                    Order = order++,
                    ParentKey = parentKey,
                    Metadata = new Dictionary<string, object>
                    {
                        ["path"] = rootPath,
                        ["value_type"] = "string",
                        ["length"] = value.Length
                    }
                });
            }
        }

        private bool ShouldCreateFragment(JsonElement element)
        {
            // Create fragments for complex types or long strings
            return element.ValueKind == JsonValueKind.Object ||
                   element.ValueKind == JsonValueKind.Array ||
                   (element.ValueKind == JsonValueKind.String && element.GetString()?.Length > 20);
        }

        private string GetValuePreview(JsonElement element, int maxLength = 50)
        {
            var value = GetValueString(element);
            if (value.Length > maxLength)
            {
                return value.Substring(0, maxLength) + "...";
            }
            return value;
        }

        private string GetValueString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "null",
                JsonValueKind.Object => $"{{...}}",
                JsonValueKind.Array => $"[{element.GetArrayLength()} items]",
                _ => element.GetRawText()
            };
        }

        public override bool CanParse(string payload)
        {
            if (!base.CanParse(payload))
                return false;

            try
            {
                using var document = JsonDocument.Parse(payload);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
