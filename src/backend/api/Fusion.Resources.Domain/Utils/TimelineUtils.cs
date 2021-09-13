using Fusion.Resources.Domain.Queries;
using Fusion.Resources.Domain.Timeline;
using Itenso.TimePeriod;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Domain
{

    public class PersonnelTimelineBuilder
    {
        private DateTime filterStart, filterEnd;
        private Timeline<QueryPersonnelTimelineItem> timeline;

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


    public static class TimelineUtils
    {
        public static IEnumerable<QueryTimelineRange<QueryRequestsTimelineItem>> GenerateRequestsTimeline(
            List<QueryResourceAllocationRequest> requests,
            DateTime filterStart,
            DateTime filterEnd)
        {
            var segments = new List<QueryTimelineRange<QueryRequestsTimelineItem>>();
            var filterPeriod = new TimeRange(filterStart, filterEnd);

            var applicableRequests = requests
                .Where(r => r.OrgPositionInstance is not null
                    && new TimeRange(r.OrgPositionInstance!.AppliesTo, r.OrgPositionInstance!.AppliesFrom).OverlapsWith(filterPeriod))
                .ToList();

            var timeline = new Timeline<QueryRequestsTimelineItem>(x => x.AppliesFrom.Date, x => x.AppliesTo.Date);
            foreach (var req in applicableRequests)
            {
                timeline.Add(new QueryRequestsTimelineItem(req));
            }

            var view = timeline.GetView(filterStart, filterEnd);
            return view.Segments.Select(x => new QueryTimelineRange<QueryRequestsTimelineItem>(x.FromDate, x.ToDate)
            {
                Items = x.Items,
                Workload = x.Items.Sum(item => item.Workload ?? 0)
            });
        }

        public static IEnumerable<QueryTimelineRange<QueryTbnPositionTimelineItem>> GenerateTbnPositionsTimeline(
           IEnumerable<QueryTbnPosition> tbnPositions,
           DateTime filterStart,
           DateTime filterEnd)
        {
            var segments = new List<QueryTimelineRange<QueryTbnPositionTimelineItem>>();
            var filterPeriod = new TimeRange(filterStart, filterEnd);

            var filteredPositions = tbnPositions
                .Where(r => new TimeRange(r.AppliesFrom, r.AppliesTo).OverlapsWith(filterPeriod))
                .ToList();

            var timeline = new Timeline<QueryTbnPosition>(x => x.AppliesFrom.Date, x => x.AppliesTo.Date);
            foreach (var position in filteredPositions)
            {
                timeline.Add(position);
            }

            var view = timeline.GetView(filterStart, filterEnd);
            return view.Segments.Select(x => new QueryTimelineRange<QueryTbnPositionTimelineItem>(x.FromDate, x.ToDate)
            {
                Items = x.Items.Select(r => new QueryTbnPositionTimelineItem(r)).ToList(),
                Workload = x.Items.Sum(item => item.Workload ?? 0)
            });
        }

        public static IEnumerable<QueryResourceAllocationRequest> FilterRequests(List<QueryResourceAllocationRequest> requests, TimeRange timelineRange)
        {
            var affectedItems = requests.Where(r =>
            {
                if (r.OrgPositionInstance == null) return false;
                var requestTimeRange = new TimeRange(r.OrgPositionInstance.AppliesFrom.Date, r.OrgPositionInstance.AppliesTo.Date);
                return requestTimeRange.OverlapsWith(timelineRange);
            });

            return affectedItems;
        }
    }
}
