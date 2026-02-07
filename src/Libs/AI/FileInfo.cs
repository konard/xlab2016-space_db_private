using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    public class FileInfo
    {
        [JsonProperty("filePath")]
        public string? FilePath { get; set; }
        [JsonProperty("size")]
        public long? Size { get; set; }
        [JsonProperty("mimeType")]
        public string? MimeType { get; set; }
    }
}
