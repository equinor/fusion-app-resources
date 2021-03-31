using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Text.Json;

namespace Fusion.Resources
{
    public static class DictionaryExtensions
    {
        private static JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented
        };

        public static string? SerializeToStringOrDefault(this Dictionary<string, object>? properties)
        {
            if (properties is null)
                return null;

            var propertiesJsonTest = JsonConvert.SerializeObject(properties, serializerSettings);
            return propertiesJsonTest;
        }
    }
}