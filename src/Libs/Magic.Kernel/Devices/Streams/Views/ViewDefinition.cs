using Magic.Kernel.Devices.Streams.Drivers;

namespace Magic.Kernel.Devices.Streams.Views
{
    /// <summary>
    /// Represents a view definition parsed from an AGI view type.
    /// A view is a named page served by a site stream, with optional fields, buttons, and a Render() method.
    /// Example AGI:
    /// <code>
    /// Login{} : view {
    ///   Username: field&lt;string&gt;(label: "Username");
    ///   Password: field&lt;string&gt;(type: "password");
    ///   Logon: button;
    ///   Error: bool;
    ///
    ///   method Render() {
    ///     return html: &lt;html&gt;...&lt;/html&gt;;
    ///   }
    /// }
    /// </code>
    /// </summary>
    public sealed class ViewDefinition
    {
        /// <summary>View name (e.g. "Login"). Used for URL routing: /login, /Login.</summary>
        public string Name { get; set; } = "";

        /// <summary>HTML content returned by the view's Render() method, pre-parsed to an AST.</summary>
        public HtmlNode? RenderResult { get; set; }

        /// <summary>Raw HTML string from Render() if AST parsing is bypassed.</summary>
        public string? RawHtml { get; set; }

        /// <summary>Fields declared in the view (Username, Password, Error, etc.).</summary>
        public List<ViewField> Fields { get; set; } = new();

        /// <summary>Buttons declared in the view.</summary>
        public List<string> Buttons { get; set; } = new();

        /// <summary>Returns the rendered HTML for this view using the <see cref="RenderDriver"/>.</summary>
        public string RenderHtml()
        {
            if (RenderResult != null)
                return RenderDriver.RenderToHtml(RenderResult);
            if (!string.IsNullOrEmpty(RawHtml))
                return RawHtml!;
            return $"<html><body><h1>{Name}</h1></body></html>";
        }
    }

    /// <summary>A field declared inside a view (e.g. <c>Username: field&lt;string&gt;(label: "Username")</c>).</summary>
    public sealed class ViewField
    {
        /// <summary>Field name (e.g. "Username", "Password").</summary>
        public string Name { get; set; } = "";

        /// <summary>Field data type (e.g. "string", "bool", "int").</summary>
        public string FieldType { get; set; } = "string";

        /// <summary>Optional display label.</summary>
        public string? Label { get; set; }

        /// <summary>Optional HTML input type override (e.g. "password", "email", "number").</summary>
        public string? InputType { get; set; }
    }
}
