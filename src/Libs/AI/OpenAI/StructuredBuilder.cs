using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.OpenAI
{
    public static class StructuredBuilder
    {
        public static List<object> Build(this List<FunctionSchema> functions)
        {
            return functions.Select(_ => Build(_)).ToList();
        }

        public static object Build(this FunctionSchema function)
        {
            var result = (dynamic)new ExpandoObject();
            result.name = function.Name;
            
            if (!string.IsNullOrEmpty(function.Description))
                result.description = function.Description;

            if (function.Parameters != null)
                result.parameters = Build(function.Parameters);

            return result;
        }

        public static object BuildJsonSchema(this FunctionSchema.ObjectSchema @object)
        {
            var result = @object.Build();

            return new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "schema",
                    schema = result,
                    strict = true
                }
            };
        }

        public static object Build(this FunctionSchema.ObjectSchema @object)
        {
            var result = (dynamic)new ExpandoObject();
            result.type = @object.Type;
            result.additionalProperties = @object.AdditionalProperties;
            if (!string.IsNullOrEmpty(@object.Description))
                result.description = @object.Description;

            if (@object.Required?.Count > 0)
                result.required = @object.Required;

            var properties = (IDictionary<string, object>)new ExpandoObject();

            if (@object.Properties?.Count > 0)
            {
                foreach (var item in @object.Properties)
                {
                    properties[item.Name] = Build(item.Body);
                }
                result.properties = properties;
            }

            if (@object.Items != null)
                result.items = Build(@object.Items);

            return result;
        }
    }
}
