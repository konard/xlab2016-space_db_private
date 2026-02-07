using AI.Interruptions;
using AI.OpenAI;

namespace AI
{
    public class ChannelChatStreamResponse
    {
        public string? Content { get; set; }
        public bool? Finished { get; set; }
    }
}
