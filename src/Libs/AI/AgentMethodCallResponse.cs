using Newtonsoft.Json;

namespace AI
{
    public class AgentMethodCallResponse
    {
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("result")]
        public string Result { get; set; }
        [JsonProperty("state")]
        public string State { get; set; }
        public bool Interrupt { get; set; }
    }
}
