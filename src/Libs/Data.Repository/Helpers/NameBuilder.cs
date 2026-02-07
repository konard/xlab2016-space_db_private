using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Helpers
{
    public class NameBuilder
    {
        private StringBuilder builder;

        public NameBuilder()
        {
            builder = new StringBuilder();
        }

        public static string Build(string[] items, string divider = ", ")
        {
            var builder = new NameBuilder();
            builder.Append(items, divider);
            return builder.ToString();
        }

        public void Append(string[] items, string divider = ", ")
        {
            if (string.IsNullOrEmpty(divider))
                divider = ", ";

            builder.Clear();

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item))
                    continue;

                if (builder.Length > 0)
                    builder.Append(divider);
                builder.Append(item);
            }
        }

        public override string ToString()
        {
            return builder.ToString();
        }
    }
}
