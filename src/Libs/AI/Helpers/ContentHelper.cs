using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Helpers
{
    public static class ContentHelper
    {
        public static string Extract(this string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            var lines = content.Split(new[] { Environment.NewLine, "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0) return content;

            if (lines[0].StartsWith("```"))
                return string.Join(Environment.NewLine, lines.Skip(1).SkipLast(1));

            return content;
        }
    }
}
