using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    public class AgentInfo
    {
        [JsonProperty("agentName")]
        public string? AgentName { get; set; }
        [JsonProperty("stateId")]
        public int? StateId { get; set; }
        [JsonProperty("state")]
        public string? State { get; set; }
        [JsonProperty("agentStack")]
        public string? AgentStack { get; set; }

        [JsonProperty("options")]
        public AgentOptions? Options { get; set; }
    }
}
