using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Fusion.Resources.Logic.Commands
{
    public static class DictionaryExtensions
    {
        public static string SerializeToString(this Dictionary<string, object>? properties)
        {
            var propertiesJson = JsonSerializer.Serialize(properties ?? new Dictionary<string, object>(),
                new JsonSerializerOptions
                    { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            return propertiesJson;
        }
        public static T DictionaryToObject<T>(this IDictionary<string, string> dict) where T : new()
        {
            var t = new T();
            PropertyInfo[] properties = t.GetType().GetProperties();
 
            foreach (PropertyInfo property in properties)
            {
                if (!dict.Any(x => x.Key.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase)))
                    continue;
 
                var item = dict.First(x => x.Key.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase));
 
                Type propertyType = t.GetType().GetProperty(property.Name)!.PropertyType;
 
                Type conversionType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
 
                object newValue = Convert.ChangeType(item.Value, conversionType);
                t.GetType().GetProperty(property.Name)!.SetValue(t, newValue, null);
            }
            return t;
        }
    }
}