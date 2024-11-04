using System;

namespace Fusion.Resources
{
    public static class CompareUtils
    {
        public static bool ContainsNullSafe(this string? value, string? comp)
        {
            return (value ?? "").Contains(comp ?? "", StringComparison.OrdinalIgnoreCase);
        }
    }
}