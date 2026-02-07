using Api.AspNetCore.Models.Configuration;

namespace AI.Client.Configuration
{
    public class QAiConnection : ServiceConnectionBase
    {
        public string ApiToken { get; set; } = string.Empty;
    }
}
