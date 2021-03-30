using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{

    public class ApiPropertiesCollection : Dictionary<string, object>
    {
        public ApiPropertiesCollection()
        {
        }

        public ApiPropertiesCollection(Dictionary<string, object> items)
        {
            foreach (var item in items)
            {
                var loweredKey = ToLowerFirstChar(item.Key);
                Add(loweredKey, item.Value);
            }
        }

        public bool ContainsKey(string key, bool ignoreCase) => ignoreCase == false ? ContainsKey(key) : Keys.Any(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));

        public T GetProperty<T>(string key, T @default)
        {
            if (ContainsKey(key, true))
            {
                var val = this.FirstOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

                if (val.Value is T typedValue)
                    return typedValue;


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
        private static string ToLowerFirstChar(string input)
        {
            string newString = input;
            if (!string.IsNullOrEmpty(newString) && char.IsUpper(newString[0]))
                newString = char.ToLower(newString[0]) + newString.Substring(1);
            return newString;
        }
    }
}