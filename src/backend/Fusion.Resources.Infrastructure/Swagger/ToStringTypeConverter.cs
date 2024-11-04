using System.ComponentModel;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Type converter to make asp.net core convert the complext type to a string. 
    /// This is usefull when the framework explodes complext types in either query or route, when it is in reality a string that is bound by logic.
    /// 
    /// Ref:
    /// https://blog.magnusmontin.net/2020/04/03/custom-data-types-in-asp-net-core-web-apis/
    /// https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/456
    /// </summary>
    public class ToStringTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }


    }
}
