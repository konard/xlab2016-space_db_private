using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    public class UserInfo
    {
        [JsonProperty("username")]
        public string? Username {  get; set; }

        [JsonProperty("claims")]
        public Dictionary<string, string>? Claims { get; set; }
    }
}
