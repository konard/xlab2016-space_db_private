using Microsoft.Extensions.Configuration;

namespace Api.AspNetCore.Models.Configuration
{
    public class ServiceConnectionConfig
    {
        public bool Enabled { get; set; }
        public string Host { get; set; }
        public int? Port { get; set; }
        public bool IsHttps { get; set; }
        public int? Version { get; set; }

        public static T Parse<T>(IConfiguration configuration, string sectionPath,
            int? defaultPort = null)
            where T : ServiceConnectionConfig, new()
        {
            var enabled = configuration[$"{sectionPath}:Enabled"];
            var port = configuration[$"{sectionPath}:Port"];
            var isHttps = configuration[$"{sectionPath}:IsHttps"];
            var version = configuration[$"{sectionPath}:Version"];
            return new T
            {
                Enabled = !string.IsNullOrEmpty(enabled) ? bool.Parse(enabled) : true,
                Host = configuration[$"{sectionPath}:Host"],
                Port = !string.IsNullOrEmpty(port) ? int.Parse(port) : defaultPort,
                IsHttps = !string.IsNullOrEmpty(isHttps) ? bool.Parse(isHttps) : false,
                Version = !string.IsNullOrEmpty(version) ? int.Parse(version) : 1
            };
        }

        public string ToUrl()
        {
            return $"{(IsHttps ? "https" : "http")}://{Host}{(Port.HasValue ? ":" + Port : string.Empty)}/api/v{Version ?? 1}/";
        }

        public string ToUrlWithoutVersion()
        {
            return $"{(IsHttps ? "https" : "http")}://{Host}{(Port.HasValue ? ":" + Port : string.Empty)}/api/";
        }
    }
}
