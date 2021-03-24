using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic
{
    public static class JObjectExtensions
    {
        public static void SetPropertyValue<T>(this JObject jObject, Expression<Func<T, object>> propertySelector, JToken propertyValue)
        {
            var prop = GetPropertyName<T, object>(propertySelector);

            var property = jObject.Properties().FirstOrDefault(p => p.Name.EqualsIgnCase(prop.Name));

            if (property == null)
            {
                var camelCasedPropertyName = CamelCaseProperty(prop.Name);
                jObject.Add(camelCasedPropertyName, propertyValue);
            }
            else
            {
                jObject[property.Name] = propertyValue;
            }
        }

        public static void RemoveProperty(this JObject jObject, params string[] propertyNames)
        {
            foreach (var prop in propertyNames)
            {
                var jProp = jObject.Property(prop, StringComparison.OrdinalIgnoreCase);
                if (jProp is not null)
                    jProp.Remove();
            }
        }

        public static void SetPropertyValue<T>(this JObject jObject, Expression<Func<T, object>> propertySelector, object propertyValue)
        {
            var prop = GetPropertyName<T, object>(propertySelector);

            var property = jObject.Properties().FirstOrDefault(p => p.Name.EqualsIgnCase(prop.Name));

            var tempObject = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(new { prop = propertyValue }));
            jObject[property!.Name] = tempObject.Property("prop")!.Value;
        }


        public static JArray? GetPropertyCollection<T>(this JObject jObject, Expression<Func<T, object>> propertySelector)
        {
            var selectedPropertyMember = GetPropertyName<T, object>(propertySelector);
            var property = jObject.Properties().FirstOrDefault(p => p.Name.EqualsIgnCase(selectedPropertyMember.Name));

            if (property == null)
            {
                // Does not exist.. Create it
                jObject[selectedPropertyMember.Name] = new JArray();
                return jObject[selectedPropertyMember.Name] as JArray;
            }

            if (property.Value.Type == JTokenType.Null)
                property.Value = new JArray();

            return property.Value as JArray;
        }
        private static PropertyInfo GetPropertyName<T, TValue>(Expression<Func<T, TValue>> selector)
        {
            MemberExpression? body = selector.Body as MemberExpression;

            if (body == null)
            {
                UnaryExpression ubody = (UnaryExpression)selector.Body;
                body = ubody.Operand as MemberExpression;
            }

            return (PropertyInfo)body!.Member;

        }

        public static TValue GetPropertyValue<T, TValue>(this JObject jObject, Expression<Func<T, TValue>> propertySelector)
        {
            var selectedPropertyMember = GetPropertyName<T, TValue>(propertySelector);
            var property = jObject.Properties().FirstOrDefault(p => p.Name.EqualsIgnCase(selectedPropertyMember.Name));

            if (property != null)
            {
                return property.Value.ToObject<TValue>()!;
            }

            return default(TValue)!;
        }

        public static bool EqualsIgnCase(this string source, string query)
        {
            if (source == null)
                return query == null;

            return source.Equals(query, StringComparison.OrdinalIgnoreCase);
        }
        private static string CamelCaseProperty(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException("Property name cannot be null when converting to camelcase");

            return Char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
        }
    }

}
