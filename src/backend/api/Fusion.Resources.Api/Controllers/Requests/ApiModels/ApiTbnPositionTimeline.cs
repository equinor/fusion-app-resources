using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Queries;
using System.Collections.Generic;

namespace Fusion.Resources.Api.Controllers.Requests
{
    public class ApiTbnPositionTimeline
    {
        public ApiTbnPositionTimeline(QueryTbnPositionsTimeline timeline)
        {
            Positions = timeline.Positions;
            Timeline = timeline.Timeline;
        }

        public List<TbnPosition>? Positions { get; }
        public List<QueryTimelineRange<QueryTBNPositionTimelineItem>>? Timeline { get; }
    }
}
