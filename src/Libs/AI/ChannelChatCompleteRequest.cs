using AI.Interruptions;
using AI.OpenAI;

namespace AI
{
    public class ChannelChatCompleteRequest
    {
        public string ApiToken { get; set; }
        public int ChannelId { get; set; }
        public string Text { get; set; }
        public string ChatId { get; set; }
        public string? Username { get; set; }
        public int? AgentStateId { get; set; }
        public bool IsCallback { get; set; }

        public AgentChatInterrupt? Interrupt { get; set; }

        //public TransferAgentData? TransferAgentData { get; set; }
        //public FinishAgentData? FinishAgentData { get; set; }

        public ContactInfo? Contact { get; set; }
        public FileInfo? File {  get; set; }
    }
}
