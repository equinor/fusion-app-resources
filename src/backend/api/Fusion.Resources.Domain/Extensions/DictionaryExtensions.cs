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

        public static string SerializeToString(this Dictionary<string, object>? properties)
        {
            var propertiesJsonTest = JsonConvert.SerializeObject(properties, serializerSettings);
            return propertiesJsonTest;
        }
    }
}