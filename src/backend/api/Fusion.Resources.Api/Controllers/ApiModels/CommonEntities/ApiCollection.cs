using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiCollection<T>
    {
        public ApiCollection(IEnumerable<T> items)
        {
            Value = items;
        }


        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TotalCount { get; set; }

        public IEnumerable<T> Value { get; set; }
    }



}
