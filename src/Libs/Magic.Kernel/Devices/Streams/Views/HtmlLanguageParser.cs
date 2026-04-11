namespace Magic.Kernel.Devices.Streams.Views
{
    /// <summary>
    /// Parses HTML markup strings (from AGI view Render() methods) into an <see cref="HtmlNode"/> AST.
    /// Handles the <c>return html: &lt;html&gt;...&lt;/html&gt;</c> sublanguage syntax.
    /// Supports standard HTML elements, attributes, and text content.
    /// </summary>
    public sealed class HtmlLanguageParser
    {
        private readonly string _source;
        private int _pos;

        private HtmlLanguageParser(string source)
        {
            _source = source ?? "";
            _pos = 0;
        }

        /// <summary>
        /// Parses an HTML markup string and returns the root <see cref="HtmlNode"/>.
        /// The source may optionally contain the <c>html:</c> prefix (from AGI return statements).
        /// Returns null if the source is empty or contains no valid HTML.
        /// </summary>
        public static HtmlNode? Parse(string? htmlSource)
        {
            if (string.IsNullOrWhiteSpace(htmlSource))
                return null;

            // Strip optional "html:" prefix from AGI return statement.
            var src = htmlSource.Trim();
            if (src.StartsWith("html:", StringComparison.OrdinalIgnoreCase))
                src = src.Substring(5).TrimStart();

            // Strip trailing semicolon if present.
            if (src.EndsWith(";"))
                src = src.Substring(0, src.Length - 1).TrimEnd();

            if (string.IsNullOrWhiteSpace(src))
                return null;

            var parser = new HtmlLanguageParser(src);
            return parser.ParseDocument();
        }

        private HtmlNode? ParseDocument()
        {
            SkipWhitespace();
            if (_pos >= _source.Length)
                return null;

            // Parse the first element as the root.
            return ParseElement();
        }

        private HtmlNode? ParseElement()
        {
            SkipWhitespace();
            if (_pos >= _source.Length)
                return null;

            if (_source[_pos] != '<')
            {
                // Text node.
                return ParseTextNode();
            }

            // Check for closing tag — should not happen here.
            if (_pos + 1 < _source.Length && _source[_pos + 1] == '/')
                return null;

            // Check for comment <!-- ... -->.
            if (_pos + 3 < _source.Length && _source.Substring(_pos, 4) == "<!--")
            {
                SkipComment();
                return ParseElement();
            }

            // Opening tag.
            _pos++; // skip '<'
            SkipWhitespace();

            var tagName = ReadIdentifier();
            if (string.IsNullOrEmpty(tagName))
                return null;

            var node = new HtmlNode(tagName.ToLowerInvariant());

            // Parse attributes.
            SkipWhitespace();
            while (_pos < _source.Length && _source[_pos] != '>' && !(_pos + 1 < _source.Length && _source[_pos] == '/' && _source[_pos + 1] == '>'))
            {
                var attr = ParseAttribute();
                if (attr != null)
                    node.Attributes.Add(attr);
                else
                    break;
                SkipWhitespace();
            }

            // Self-closing tag <br/> or <input/>.
            if (_pos + 1 < _source.Length && _source[_pos] == '/' && _source[_pos + 1] == '>')
            {
                _pos += 2;
                return node;
            }

            if (_pos < _source.Length && _source[_pos] == '>')
                _pos++; // skip '>'

            // Parse children (for non-void elements).
            if (!IsVoidElement(tagName))
            {
                ParseChildren(node, tagName);
            }

            return node;
        }

        private void ParseChildren(HtmlNode parent, string parentTagName)
        {
            while (_pos < _source.Length)
            {
                SkipWhitespace();
                if (_pos >= _source.Length)
                    break;

                // Check for closing tag.
                if (_pos + 1 < _source.Length && _source[_pos] == '<' && _source[_pos + 1] == '/')
                {
                    _pos += 2; // skip '</'
                    var closingTag = ReadIdentifier();
                    // Skip to end of closing tag.
                    while (_pos < _source.Length && _source[_pos] != '>')
                        _pos++;
                    if (_pos < _source.Length)
                        _pos++; // skip '>'
                    break;
                }

                // Check for comment.
                if (_pos + 3 < _source.Length && _source.Substring(_pos, 4) == "<!--")
                {
                    SkipComment();
                    continue;
                }

                if (_source[_pos] == '<')
                {
                    var child = ParseElement();
                    if (child != null)
                        parent.Elements.Add(child);
                }
                else
                {
                    var text = ParseInlineText();
                    if (!string.IsNullOrWhiteSpace(text))
                        parent.Elements.Add(HtmlNode.CreateText(text.Trim()));
                }
            }
        }

        private HtmlNode? ParseTextNode()
        {
            var text = ParseInlineText();
            return string.IsNullOrWhiteSpace(text) ? null : HtmlNode.CreateText(text.Trim());
        }

        private string ParseInlineText()
        {
            var sb = new System.Text.StringBuilder();
            while (_pos < _source.Length && _source[_pos] != '<')
            {
                sb.Append(_source[_pos]);
                _pos++;
            }
            return sb.ToString();
        }

        private HtmlAttribute? ParseAttribute()
        {
            SkipWhitespace();
            if (_pos >= _source.Length || _source[_pos] == '>' || (_source[_pos] == '/' && _pos + 1 < _source.Length && _source[_pos + 1] == '>'))
                return null;

            var name = ReadAttributeName();
            if (string.IsNullOrEmpty(name))
                return null;

            SkipWhitespace();

            // Boolean attribute (no value).
            if (_pos >= _source.Length || _source[_pos] != '=')
                return new HtmlAttribute(name.ToLowerInvariant());

            _pos++; // skip '='
            SkipWhitespace();

            var value = ReadAttributeValue();
            return new HtmlAttribute(name.ToLowerInvariant(), value);
        }

        private string ReadIdentifier()
        {
            var start = _pos;
            while (_pos < _source.Length && (char.IsLetterOrDigit(_source[_pos]) || _source[_pos] == '-' || _source[_pos] == '_' || _source[_pos] == ':'))
                _pos++;
            return _source.Substring(start, _pos - start);
        }

        private string ReadAttributeName()
        {
            var start = _pos;
            while (_pos < _source.Length && _source[_pos] != '=' && _source[_pos] != '>' && _source[_pos] != '/' && !char.IsWhiteSpace(_source[_pos]))
                _pos++;
            return _source.Substring(start, _pos - start);
        }

        private string ReadAttributeValue()
        {
            if (_pos >= _source.Length)
                return "";

            char quote = _source[_pos];
            if (quote == '"' || quote == '\'')
            {
                _pos++; // skip opening quote
                var start = _pos;
                while (_pos < _source.Length && _source[_pos] != quote)
                    _pos++;
                var val = _source.Substring(start, _pos - start);
                if (_pos < _source.Length)
                    _pos++; // skip closing quote
                return val;
            }

            // Unquoted value.
            var startU = _pos;
            while (_pos < _source.Length && !char.IsWhiteSpace(_source[_pos]) && _source[_pos] != '>')
                _pos++;
            return _source.Substring(startU, _pos - startU);
        }

        private void SkipWhitespace()
        {
            while (_pos < _source.Length && char.IsWhiteSpace(_source[_pos]))
                _pos++;
        }

        private void SkipComment()
        {
            // Skip <!-- ... -->
            _pos += 4; // skip "<!--"
            while (_pos + 2 < _source.Length)
            {
                if (_source[_pos] == '-' && _source[_pos + 1] == '-' && _source[_pos + 2] == '>')
                {
                    _pos += 3;
                    return;
                }
                _pos++;
            }
        }

        private static bool IsVoidElement(string tagName)
        {
            // HTML void elements (self-closing, no children).
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
