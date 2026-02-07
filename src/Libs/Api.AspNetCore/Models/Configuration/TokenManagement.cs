using Newtonsoft.Json;

namespace Api.AspNetCore.Models.Configuration
{
    [JsonObject("tokenManagement")]
    public class TokenManagement
    {
        [JsonProperty("secret")]
        public string Secret { get; set; }

        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        [JsonProperty("audience")]
        public string Audience { get; set; }

        [JsonProperty("refreshExpiration")]
        public int RefreshExpiration { get; set; }
        [JsonProperty("authority")]
        public string Authority { get; set; }
    }
}
