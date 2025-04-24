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
        public int? Top { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Skip { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TotalCount { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? HasMoreItems => TotalCount is not null && Skip is not null && Top is not null
            ? TotalCount > Skip + Top
            : null;

        public IEnumerable<T> Value { get; set; }
    }



}
