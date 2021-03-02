using System.Collections.Generic;
using System.Text.Json;

namespace Fusion.Resources.Logic.Commands
{
    public static class DictionaryExtensions
    {
        public static string SerializeToString(this Dictionary<string, object>? properties)
        {
            var propertiesJson = JsonSerializer.Serialize(properties ?? new Dictionary<string, object>(),
                new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            return propertiesJson;
        }
    }
}