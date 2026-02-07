using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    public class AIMarkup
    {
        [JsonProperty("keyboard")]
        public AKeyboard? Keyboard { get; set; }
        [JsonProperty("media")]
        public AMedia? Media { get; set; }

        public class AKeyboard
        {
            [JsonProperty("buttons")]

            public List<AButton>? Buttons { get; set; }
            [JsonProperty("rows")]
            public List<ARow> Rows { get; set; }

            public bool Inline { get; set; }
            public bool Clear {  get; set; }
            public bool Remove {  get; set; }
        }

        public class AMedia
        {
            [JsonProperty("type")]
            public AMediaType? Type { get; set; }
            [JsonProperty("align")]
            public AMediaAlign? Align { get; set; }
            [JsonProperty("contentBase64")]
            public string? Base64Content {  get; set; }
            public int? Length {  get; set; }
        }

        public enum AMediaType : int
        {
            Undefined = 0,
            Video = 1,
            Audio = 2,
            VideoNote = 3
        }

        public enum AMediaAlign : int
        {
            Undefined = 0,
            Top = 1,
            Bottom = 2
        }

        public class ARow
        {
            [JsonProperty("buttons")]
            public List<AButton>? Buttons { get; set; }
        }

        public class AButton
        {
            [JsonProperty("label")]
            public string? Label { get; set; }
            [JsonProperty("data")]
            public string? Data { get; set; }
            [JsonProperty("url")]
            public string? Url { get; set; }
            [JsonProperty("handler")]
            public string? Handler { get; set; }

            public const string RequestContactHandler = "RequestContact";
        }
    }
}
