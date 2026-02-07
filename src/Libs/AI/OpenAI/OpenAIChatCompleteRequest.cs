using Newtonsoft.Json;

namespace AI.OpenAI
{
    public class OpenAIChatCompleteRequest
    {
        [JsonProperty("apiToken")]
        public string? ApiToken { get; set; }
        [JsonProperty("temperature")]
        public decimal? Temperature {  get; set; }
        [JsonProperty("response_format")]
        public object? ResponseFormat { get; set; }
        [JsonProperty("k")]
        public int? K {  get; set; }
        [JsonProperty("messages")]
        public List<OpenAIMessage> Messages {  get; set; }
        [JsonProperty("functions")]
        public List<object>? Functions {  get; set; }
    }
}
