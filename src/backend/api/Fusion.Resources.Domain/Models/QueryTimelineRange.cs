using System;
using System.Collections.Generic;

namespace Fusion.Resources.Domain
{
    public class QueryTimelineRange<TItem>
    {
        public QueryTimelineRange(DateTime from, DateTime to)
        {
            AppliesFrom = from;
            AppliesTo = to;
        }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public List<TItem> Items { get; set; } = new List<TItem>();
        public double Workload { get; set; }
    }
}
