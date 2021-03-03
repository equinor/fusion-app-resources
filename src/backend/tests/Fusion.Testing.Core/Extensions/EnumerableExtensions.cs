using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Testing
{
    public static class EnumerableExtensions
    {
        private static Random random = new Random();

        public static T PickRandom<T>(this IEnumerable<T> items)
        {
            return items.ElementAt(random.Next(0, items.Count() - 1));
        }
    }
}
