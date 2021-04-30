using Fusion.AspNetCore.OData;
using System;
using System.Linq;

namespace Fusion.Resources.Domain
{
    public static class ODataParamsExtensions
    {
        public static bool ShouldExpand(this ODataQueryParams query, string property)
        {
            if (query.Expand != null && query.Expand.Contains(property, StringComparer.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}
