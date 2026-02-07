using AI.Interruptions;
using AI.OpenAI;

namespace AI
{
    public class ChannelChatStreamRequest
    {
        public int ChannelId { get; set; }        
        public string ChatId { get; set; }
    }
}
