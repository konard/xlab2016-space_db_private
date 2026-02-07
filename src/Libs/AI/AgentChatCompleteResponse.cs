using AI.OpenAI;

namespace AI
{
    public class AgentChatCompleteResponse
    {
        public string AgentName { get; set; }
        public string State { get; set; }
        public string? Data { get; set; }
        public long? AgentRunId {  get; set; }
        public bool EscapeSpecialCharacters { get; set; } = true; // Escape special characters
        public OpenAIMessage? FunctionCallMessage { get; set; }
        public OpenAIMessage? Message { get; set; }
        public AgentChatInterrupt? Interrupt { get; set; }
        public AIMarkup? Markup { get; set; }
        public List<OpenAIMessage> Messages { get; set; }

        public List<OpenAIMessage>? AffectedMessages {  get; set; }

        public string? Error { get; set; }
    }
}
