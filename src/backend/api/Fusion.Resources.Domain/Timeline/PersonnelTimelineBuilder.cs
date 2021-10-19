using Fusion.Resources.Domain.Timeline;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Domain
{
    public class PersonnelTimelineBuilder
    {
        private readonly DateTime filterStart;
        private readonly DateTime filterEnd;
        private readonly Timeline<QueryPersonnelTimelineItem> timeline;

        public PersonnelTimelineBuilder(DateTime filterStart, DateTime filterEnd)
        {
            if (filterStart.Kind != DateTimeKind.Utc)
                filterStart = DateTime.SpecifyKind(filterStart, DateTimeKind.Utc);

            if (filterEnd.Kind != DateTimeKind.Utc)
                filterEnd = DateTime.SpecifyKind(filterEnd, DateTimeKind.Utc);

            this.filterStart = filterStart;
            this.filterEnd = filterEnd;
            timeline = new Timeline<QueryPersonnelTimelineItem>(x => x.AppliesFrom.Date, x => x.AppliesTo.Date);
        }

        public PersonnelTimelineBuilder WithPositions(List<QueryPersonnelPosition> positions)
        {
            positions.ForEach(p => timeline.Add(new QueryPersonnelTimelineItem("PositionInstance", p)));
            return this;
        }

        public PersonnelTimelineBuilder WithAbsences(List<QueryPersonAbsenceBasic> absences)
        {
            absences.ForEach(a => timeline.Add(new QueryPersonnelTimelineItem("Absence", a)));
            return this;
        }
        public PersonnelTimelineBuilder WithPendingRequests(List<QueryResourceAllocationRequest> pendingRequests)
        {
            pendingRequests.ForEach(rq => timeline.Add(new QueryPersonnelTimelineItem("Request", rq)));
            return this;
        }

        public List<QueryTimelineRange<QueryPersonnelTimelineItem>> Build()
        {
            var view = timeline.GetView(filterStart, filterEnd);
            return view.Segments.Select(x => new QueryTimelineRange<QueryPersonnelTimelineItem>(x.FromDate, x.ToDate)
            {
                Items = x.Items,
                Workload = x.Items.Sum(item => item.Workload ?? 0)
            })
            .OrderBy(x => x.AppliesFrom)
            .ToList();
        }
    }
}
