using Newtonsoft.Json;

namespace AI
{
    public class AgentMethodCallRequest
    {
        [JsonProperty("request")]
        public AgentChatCompleteRequest? Request { get; set; }
        [JsonProperty("agentName")]
        public string? AgentName { get; set; }
        [JsonProperty("functionName")]
        public string? FunctionName { get; set; }
        [JsonProperty("args")]
        public string? Args { get; set; }
        [JsonProperty("functionsUrl")]
        public string? FunctionsUrl { get; set; }

        [JsonProperty("user")]
        public UserInfo? User { get; set; }
    }
}
