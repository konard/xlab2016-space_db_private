using Newtonsoft.Json;

namespace AI.Recognize
{
    public class Picture
    {
        [JsonProperty("contentBase64")]
        public string ContentBase64 { get; set; }
    }
}
