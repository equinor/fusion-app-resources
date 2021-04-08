using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Application.LineOrg
{
    internal class PaginatedResponse<T>
    {
        public int Count { get; set; }
        public int TotalCount { get; set; }

        [JsonPropertyName("@NextPage")]
        public string? NextPage { get; set; }
        public List<T> Value { get; set; } = new List<T>();
    }
}
