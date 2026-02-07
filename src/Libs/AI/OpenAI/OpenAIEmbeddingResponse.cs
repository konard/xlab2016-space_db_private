using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.OpenAI
{
    public class OpenAIEmbeddingResponse
    {
        [JsonProperty("embeddings")]
        public List<List<float>>? Embeddings { get; set; }

        [JsonProperty("usages")]
        public List<int>? Usages { get; set; }
    }
}
