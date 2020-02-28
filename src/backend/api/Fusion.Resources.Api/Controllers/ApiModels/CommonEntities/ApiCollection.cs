using System.Collections.Generic;

namespace Fusion.Resources.Api.Controllers
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
