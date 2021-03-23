using Fusion.Resources.Domain.Queries;
using System.Collections.Generic;

namespace Fusion.Resources.Domain
{
    public class QueryTbnPositionsTimeline
    {
        public List<QueryTimelineRange<QueryTBNPositionTimelineItem>>? Timeline { get; set; }
        public List<TbnPosition>? Positions { get; set; }

    }
}
