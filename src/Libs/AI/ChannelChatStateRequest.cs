using AI.Interruptions;
using AI.OpenAI;

namespace AI
{
    public class ChannelChatStateRequest
    {
        public string ApiToken { get; set; }
        public int ChannelId { get; set; }
        public string Text { get; set; }
        public string ChatId { get; set; }
        public string? Username { get; set; }

        public ContactInfo? Contact { get; set; }
        public FileInfo? File {  get; set; }
    }
}
