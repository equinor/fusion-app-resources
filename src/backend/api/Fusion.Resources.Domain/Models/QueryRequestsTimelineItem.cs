using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class QueryRequestsTimelineItem
    {
        public string Id { get; set; }
        public string? PositionName { get; set; }
        public string? ProjectName { get; set; }
        public double? Workload { get; set; }
    }
}
