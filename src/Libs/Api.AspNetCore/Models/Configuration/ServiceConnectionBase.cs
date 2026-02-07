using Microsoft.Extensions.Configuration;

namespace Api.AspNetCore.Models.Configuration
{
    public class ServiceConnectionBase
    {
        public string Host { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int? Version { get; set; }

        public static T Parse<T>(IConfiguration configuration, string sectionPath)
            where T : ServiceConnectionBase, new()
        {
            var version = configuration[$"{sectionPath}:Version"];
            return new T
            {
                Host = configuration[$"{sectionPath}:Host"],
                UserName = configuration[$"{sectionPath}:UserName"],
                Password = configuration[$"{sectionPath}:Password"],
                Version = !string.IsNullOrEmpty(version) ? int.Parse(version) : 1
            };
        }
    }
}
