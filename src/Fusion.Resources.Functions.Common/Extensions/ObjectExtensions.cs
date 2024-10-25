using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Common.Extensions;

public static class ObjectExtensions
{
    public static string ToJson(this object obj)
    {
        return JsonConvert.SerializeObject(obj, Formatting.Indented);
    }
}