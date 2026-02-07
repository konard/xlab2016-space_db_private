using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Api.AspNetCore.Helpers
{
    public static class JsonSerializerSettingsDefault
    {
        public static JsonSerializerSettings Value { get; set; }

        static JsonSerializerSettingsDefault()
        {
            Value = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
        }
    }
}
