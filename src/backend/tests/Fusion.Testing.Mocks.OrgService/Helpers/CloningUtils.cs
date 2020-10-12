using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fusion.Testing.Mocks.OrgService
{
    public static class CloningUtils
    {
        /// <summary>
        /// Will use JsonConvert to "round-trip" the object.
        /// </summary>
        public static T JsonClone<T>(this T item)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(item));
        }
    }
}
