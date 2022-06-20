using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fusion.Resources.Domain
{
    public class JObjectProxy<T>
    {
        public JObjectProxy()
        {
            JsonObject = new JObject();
        }
        public JObjectProxy(JObject jsonObject)
        {
            JsonObject = jsonObject;
        }
        public JObjectProxy(T @object)
        {
            if (@object is null) throw new ArgumentNullException(nameof(@object));

            JsonObject = new JObject(@object);
        }

        public JObject JsonObject { get; }

        public void SetPropertyValue(Expression<Func<T, object>> propertySelector, JToken propertyValue)
        {
            var pocoProperty = GetPropertyName(propertySelector);
            var jsonProperty = JsonObject.Property(pocoProperty.Name, StringComparison.OrdinalIgnoreCase);

            if (jsonProperty == null)
            {
                var camelCasedPropertyName = CamelCaseProperty(pocoProperty.Name);
                JsonObject.Add(camelCasedPropertyName, propertyValue);
            }
            else
            {
                JsonObject[jsonProperty.Name] = propertyValue;
            }
        }

        public void SetPropertyValue(Expression<Func<T, object>> propertySelector, object propertyValue)
        {
            var pocoProperty = GetPropertyName(propertySelector);
            var jsonProperty = JsonObject.Property(pocoProperty.Name, StringComparison.OrdinalIgnoreCase);

            var tempObject = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(new { prop = propertyValue }));

            if (tempObject is null)
                return;

            if (jsonProperty == null)
            {
                var camelCasedPropertyName = CamelCaseProperty(pocoProperty.Name);
                JsonObject.Add(camelCasedPropertyName, tempObject.Property("prop")!.Value);
            }
            else
            {
                JsonObject[jsonProperty.Name] = tempObject.Property("prop")!.Value;
            }
        }

        private static PropertyInfo GetPropertyName<TValue>(Expression<Func<T, TValue>> selector)
        {
            MemberExpression? body = selector.Body as MemberExpression;

            if (body == null)
            {
                UnaryExpression ubody = (UnaryExpression)selector.Body;
                body = ubody.Operand as MemberExpression;
            }

            return (PropertyInfo)body!.Member;

        }

        public JArray? GetPropertyCollection(Expression<Func<T, object>> propertySelector)
        {
            var selectedPropertyMember = GetPropertyName(propertySelector);
            var property = JsonObject.Property(selectedPropertyMember.Name, StringComparison.OrdinalIgnoreCase);

            if (property == null)
            {
                // Does not exist.. Create it
                JsonObject[selectedPropertyMember.Name] = new JArray();
                return JsonObject[selectedPropertyMember.Name] as JArray;
            }

            if (property.Value.Type == JTokenType.Null)
                property.Value = new JArray();

            return property.Value as JArray;
        }

        public TValue GetPropertyValue<TValue>(Expression<Func<T, TValue>> propertySelector)
        {
            var selectedPropertyMember = GetPropertyName(propertySelector);
            var property = JsonObject.Property(selectedPropertyMember.Name, StringComparison.OrdinalIgnoreCase);

            if (property != null)
            {
                return property.Value.ToObject<TValue>()!;
            }

            return default!;
        }

        public void RemoveProperty(params string[] propertyNames)
        {
            foreach (var prop in propertyNames)
            {
                var jProp = JsonObject.Property(prop, StringComparison.OrdinalIgnoreCase);
                if (jProp is not null)
                    jProp.Remove();
            }
        }

        private static string CamelCaseProperty(string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException("Property name cannot be null when converting to camelcase");

            return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        }

        public JObjectProxy<T> Clone()
        {
            return new JObjectProxy<T>((JObject)JsonObject.DeepClone());
        }
    }
}
