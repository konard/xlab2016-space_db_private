using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    public class AgentRunInfo
    {
        [JsonProperty("id")]
        public long? Id {  get; set; }
        [JsonProperty("data")]
        public string? Data { get; set; }
    }
}
