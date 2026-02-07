using Newtonsoft.Json;

namespace AI.OpenAI
{
    public class OpenAIMessage
    {
        [JsonProperty("role")]
        public string? Role { get; set; }
        [JsonProperty("name")]
        public string? Name { get; set; }
        [JsonProperty("content")]        
        public string? Content { get; set; }
        [JsonProperty("total_tokens")]
        public int? TotalTokens { get; set; }
        [JsonProperty("function_call")]
        public OpenAIFunctionCall? FunctionCall { get; set; }
    }
}
