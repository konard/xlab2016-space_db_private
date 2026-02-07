using Newtonsoft.Json;

namespace AI.OpenAI
{
    public class OpenAIChatCompleteResponse
    {
        [JsonProperty("result")]
        public OpenAIMessage? Result {  get; set; }

        [JsonProperty("total_tokens")]
        public int? TotalTokens { get; set; }
    }
}
