using System.Text.Json;

namespace Magic.Kernel.Devices.Streams.Views
{
    /// <summary>
    /// Projects an <see cref="HtmlNode"/> AST to a JSON-serializable dictionary structure.
    /// Output format per node:
    /// <code>
    /// {
    ///   "name": "element_name",
    ///   "elements": [ ...child nodes... ],
    ///   "attributes": [ { "name": "attr", "value": "val" }, ... ]
    /// }
    /// </code>
    /// </summary>
    public static class HtmlAstProjector
    {
        /// <summary>
        /// Projects an <see cref="HtmlNode"/> AST to a JSON-compatible dictionary.
        /// Text nodes are represented as <c>{ "name": "#text", "text": "...", "elements": [], "attributes": [] }</c>.
        /// </summary>
        public static Dictionary<string, object?> ToJson(HtmlNode? node)
        {
            if (node == null)
                return new Dictionary<string, object?> { ["name"] = "", ["elements"] = new List<object>(), ["attributes"] = new List<object>() };

            var dict = new Dictionary<string, object?>
            {
                ["name"] = node.Name
            };

            if (node.IsTextNode)
            {
                dict["text"] = node.Text ?? "";
                dict["elements"] = new List<object>();
                dict["attributes"] = new List<object>();
                return dict;
            }

            dict["elements"] = node.Elements
                .Select(child => (object)ToJson(child))
                .ToList();

            dict["attributes"] = node.Attributes
                .Select(attr => (object)new Dictionary<string, object?>
                {
                    ["name"] = attr.Name,
                    ["value"] = attr.Value
                })
                .ToList();

            return dict;
        }

        /// <summary>Serializes the projected JSON map to a JSON string.</summary>
        public static string ToJsonString(HtmlNode? node)
        {
            var map = ToJson(node);
            return JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
