using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.OpenAI
{
    public class OpenAIEmbeddingRequest
    {
        [JsonProperty("text")]
        public List<string>? Text { get; set; }
    }
}
