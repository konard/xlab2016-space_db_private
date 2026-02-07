using Newtonsoft.Json;

namespace AI.OpenAI
{
    public class OpenAIFunctionCall
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("arguments")]
        public string? Arguments { get; set; }
    }
}
