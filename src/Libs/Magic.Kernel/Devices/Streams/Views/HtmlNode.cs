namespace Magic.Kernel.Devices.Streams.Views
{
    /// <summary>
    /// Represents a single HTML element node in the HTML AST.
    /// Maps to the JSON structure: { "name": "element_name", "elements": [], "attributes": [] }
    /// </summary>
    public sealed class HtmlNode
    {
        /// <summary>Element tag name (e.g. "html", "body", "div", "button").</summary>
        public string Name { get; set; } = "";

        /// <summary>Text content for text nodes (when Name is "#text").</summary>
        public string? Text { get; set; }

        /// <summary>Child elements.</summary>
        public List<HtmlNode> Elements { get; set; } = new();

        /// <summary>Element attributes (e.g. id="login", type="password").</summary>
        public List<HtmlAttribute> Attributes { get; set; } = new();

        /// <summary>Returns true if this is a text node.</summary>
        public bool IsTextNode => string.Equals(Name, "#text", StringComparison.OrdinalIgnoreCase);

        public HtmlNode() { }

        public HtmlNode(string name)
        {
            Name = name;
        }

        /// <summary>Creates a text node.</summary>
        public static HtmlNode CreateText(string text) =>
            new HtmlNode { Name = "#text", Text = text };

        public override string ToString() =>
            IsTextNode ? $"#text({Text})" : $"<{Name}> ({Elements.Count} children, {Attributes.Count} attrs)";
    }

    /// <summary>An HTML attribute key-value pair.</summary>
    public sealed class HtmlAttribute
    {
        /// <summary>Attribute name (e.g. "id", "class", "type").</summary>
        public string Name { get; set; } = "";

        /// <summary>Attribute value. May be null for boolean attributes (e.g. &lt;input disabled&gt;).</summary>
        public string? Value { get; set; }

        public HtmlAttribute() { }

        public HtmlAttribute(string name, string? value = null)
        {
            Name = name;
            Value = value;
        }

        public override string ToString() =>
            Value == null ? Name : $"{Name}=\"{Value}\"";
    }
}
