using System.Collections.Generic;

namespace Fusion.Resources.Domain.LineOrg
{
    public class PaginatedResponse<T>
    {
        public int Count { get; set; }
        public int TotalCount { get; set; }
        public List<T> Value { get; set; }
    }
}
