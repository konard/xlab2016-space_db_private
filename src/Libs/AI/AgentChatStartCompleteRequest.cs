using AI.Interruptions;
using AI.OpenAI;
using Newtonsoft.Json;

namespace AI
{
    public class AgentChatStartCompleteRequest
    {
        public string ApiToken { get; set; }

        public string AgentName { get; set; }
        public string State { get; set; }
        public int Version { get; set; }
        public string? CompleteUrl { get; set; }
        public string? FunctionsUrl { get; set; }
        public bool IsCallback { get; set; }

        public OpenAIMessage Message { get; set; }
        public List<OpenAIMessage> Messages { get; set; }
        public List<object>? Functions { get; set; }

        public UserInfo? User { get; set; }
        public ContactInfo? Contact { get; set; }
        public FileInfo? File { get; set; }
        public AgentInfo? Agent { get; set; }
        public AgentRunInfo? AgentRun { get; set; }
    }
}
