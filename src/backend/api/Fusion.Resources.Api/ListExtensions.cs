using System.Collections.Generic;

namespace Fusion.Resources.Api
{
    public static class ListExtensions
    {
        public static void Add<T>(this List<T> list, params T[] items) => list.AddRange(items);
    }
}
