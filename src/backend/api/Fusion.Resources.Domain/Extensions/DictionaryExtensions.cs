using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
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

        public static bool ContainsKey(this Dictionary<string, object> dictionary, string key, bool ignoreCase)
        {
            return ignoreCase == false ? dictionary.ContainsKey(key) : dictionary.Keys.Any(k => k.Equals(key, System.StringComparison.OrdinalIgnoreCase));
        }

        public static T GetProperty<T>(this Dictionary<string, object> dictionary, string key, T @default)
        {
            if (dictionary.ContainsKey(key, true))
            {
                var val = dictionary.FirstOrDefault(kv => kv.Key.Equals(key, System.StringComparison.OrdinalIgnoreCase));

                if (val.Value is T typedValue)
                    return typedValue;

                if (val.Value is null)
                    return @default;

                var type = typeof(T);
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    throw new InvalidOperationException($"Cannot get typed property. Requested type: '{type.GetGenericArguments().First().Name} or null', value type: '{val.Value.GetType().Name}'");
                }

                if (type.IsGenericType)
                {
                }

                throw new InvalidOperationException($"Cannot get typed property. Requested type: '{typeof(T).Name}', value type: '{val.GetType().Name}'");
            }

            return @default;
        }
    }
}