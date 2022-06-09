using System.Collections.Generic;

#nullable enable

namespace Fusion.Testing.Mocks
{
    public class ApiCollection<T>
    {
        public ApiCollection(IEnumerable<T> items)
        {
            Value = items;
        }

        public IEnumerable<T> Value { get; set; }
    }
}
