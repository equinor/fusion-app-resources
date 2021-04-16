using Fusion.Resources.Domain.Queries;
using System.Collections.Generic;

namespace Fusion.Resources.Domain
{
    public class QueryTbnPositionsTimeline
    {
        public QueryTbnPositionsTimeline(List<QueryTimelineRange<QueryTbnPositionTimelineItem>> timeline, List<QueryTbnPosition> relevantPositions)
        {
            Timeline = timeline;
            Positions = relevantPositions;
        }

        public List<QueryTimelineRange<QueryTbnPositionTimelineItem>> Timeline { get; set; }
        public List<QueryTbnPosition> Positions { get; set; }

    }
}
