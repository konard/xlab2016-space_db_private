using Magic.Kernel.Devices.Streams.Views;

namespace Magic.Kernel.Devices.Streams.Drivers
{
    /// <summary>
    /// Converts a JSON HTML structure (from <see cref="HtmlAstProjector"/>) into formatted HTML output.
    /// This is the rendering back-end for view definitions — later may support themes and AI-driven rendering.
    /// Usage pipeline:
    ///   AGI view Render() → HTML markup string
    ///     → HtmlLanguageParser.Parse()  → HtmlNode AST
    ///     → HtmlAstProjector.ToJson()   → JSON map
    ///     → RenderDriver.RenderToHtml() → formatted HTML string
    /// </summary>
    public static class RenderDriver
    {
        /// <summary>
        /// Converts an <see cref="HtmlNode"/> AST into a formatted HTML string with proper indentation.
        /// </summary>
        public static string RenderToHtml(HtmlNode? node, int indent = 0)
        {
            if (node == null)
                return "";

            if (node.IsTextNode)
                return new string(' ', indent * 2) + (node.Text ?? "");

            var sb = new System.Text.StringBuilder();
            var indentStr = new string(' ', indent * 2);
            var tagName = node.Name;

            // Build opening tag with attributes.
            sb.Append(indentStr);
            sb.Append('<');
            sb.Append(tagName);

            foreach (var attr in node.Attributes)
            {
                sb.Append(' ');
                sb.Append(attr.Name);
                if (attr.Value != null)
                {
                    sb.Append("=\"");
                    sb.Append(attr.Value);
                    sb.Append('"');
                }
            }

            // Self-closing void element.
            if (IsVoidElement(tagName) && node.Elements.Count == 0)
            {
                sb.Append(" />");
                return sb.ToString();
            }

            sb.Append('>');

            // If no children, close on same line.
            if (node.Elements.Count == 0)
            {
                sb.Append("</");
                sb.Append(tagName);
                sb.Append('>');
                return sb.ToString();
            }

            // Check if all children are text nodes (inline rendering).
            if (node.Elements.Count == 1 && node.Elements[0].IsTextNode)
            {
                sb.Append(node.Elements[0].Text ?? "");
                sb.Append("</");
                sb.Append(tagName);
                sb.Append('>');
                return sb.ToString();
            }

            // Multi-line rendering for element children.
            sb.AppendLine();
            foreach (var child in node.Elements)
            {
                var childHtml = RenderToHtml(child, indent + 1);
                sb.AppendLine(childHtml);
            }

            sb.Append(indentStr);
            sb.Append("</");
            sb.Append(tagName);
            sb.Append('>');

            return sb.ToString();
        }

        /// <summary>
        /// Converts a JSON map (from <see cref="HtmlAstProjector.ToJson"/>) into formatted HTML.
        /// Supports the format: { "name": "...", "elements": [...], "attributes": [...] }
        /// </summary>
        public static string RenderJsonToHtml(Dictionary<string, object?> jsonNode, int indent = 0)
        {
            if (jsonNode == null || !jsonNode.TryGetValue("name", out var nameObj))
                return "";

            var tagName = nameObj as string ?? "";
            var indentStr = new string(' ', indent * 2);

            // Text node.
            if (string.Equals(tagName, "#text", StringComparison.OrdinalIgnoreCase))
            {
                var text = jsonNode.TryGetValue("text", out var t) ? t as string ?? "" : "";
                return indentStr + text;
            }

            var sb = new System.Text.StringBuilder();
            sb.Append(indentStr);
            sb.Append('<');
            sb.Append(tagName);

            // Attributes.
            if (jsonNode.TryGetValue("attributes", out var attrsObj) && attrsObj is List<object> attrs)
            {
                foreach (var attrObj in attrs)
                {
                    if (attrObj is Dictionary<string, object?> attrDict)
                    {
                        var attrName = attrDict.TryGetValue("name", out var an) ? an as string ?? "" : "";
                        var attrValue = attrDict.TryGetValue("value", out var av) ? av as string : null;

                        if (!string.IsNullOrEmpty(attrName))
                        {
                            sb.Append(' ');
                            sb.Append(attrName);
                            if (attrValue != null)
                            {
                                sb.Append("=\"");
                                sb.Append(attrValue);
                                sb.Append('"');
                            }
                        }
                    }
                }
            }

            if (IsVoidElement(tagName))
            {
                sb.Append(" />");
                return sb.ToString();
            }

            sb.Append('>');

            // Children.
            var children = new List<Dictionary<string, object?>>();
            if (jsonNode.TryGetValue("elements", out var elemsObj) && elemsObj is List<object> elems)
            {
                foreach (var elemObj in elems)
                {
                    if (elemObj is Dictionary<string, object?> childDict)
                        children.Add(childDict);
                }
            }

            if (children.Count == 0)
            {
                sb.Append("</");
                sb.Append(tagName);
                sb.Append('>');
                return sb.ToString();
            }

            // Inline if single text child.
            if (children.Count == 1 && children[0].TryGetValue("name", out var cn) && string.Equals(cn as string, "#text", StringComparison.OrdinalIgnoreCase))
            {
                var text = children[0].TryGetValue("text", out var t) ? t as string ?? "" : "";
                sb.Append(text);
                sb.Append("</");
                sb.Append(tagName);
                sb.Append('>');
                return sb.ToString();
            }

            sb.AppendLine();
            foreach (var child in children)
            {
                sb.AppendLine(RenderJsonToHtml(child, indent + 1));
            }

            sb.Append(indentStr);
            sb.Append("</");
            sb.Append(tagName);
            sb.Append('>');

            return sb.ToString();
        }

        private static bool IsVoidElement(string tagName)
        {
            return tagName.ToLowerInvariant() switch
            {
                "area" or "base" or "br" or "col" or "embed" or "hr" or
                "img" or "input" or "link" or "meta" or "param" or
                "source" or "track" or "wbr" => true,
                _ => false
            };
        }
    }
}
