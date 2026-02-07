using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.OpenAI
{
    public class FunctionSchema
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public FunctionRole Role { get; set; }
        public int? AgentId { get; set; }

        public ObjectSchema Parameters {  get; set; }

        public class ObjectSchema
        {
            public string Type { get; set; }
            public string Description { get; set; }

            public List<string> Required { get; set; }
            public bool AdditionalProperties { get; set; } = false;

            public ObjectSchema Items {  get; set; }

            public List<PropertySchema> Properties { get; set; }
        }

        public class PropertySchema
        {
            public string Name { get; set; }

            public ObjectSchema Body {  get; set; }
        }
    }
}
