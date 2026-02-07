using AI.Recognize;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    public class RecognizeResponse
    {
        [JsonProperty("tables")]
        public List<StringTable>? Tables {  get; set; }
        [JsonProperty("pictures")]
        public List<Picture>? Pictures {  get; set; }
        [JsonProperty("text")]
        public List<string>? Text {  get; set; }
    }
}
