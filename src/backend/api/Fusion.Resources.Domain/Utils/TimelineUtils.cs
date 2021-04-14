using Fusion.Resources.Domain.Queries;
using Fusion.Resources.Domain.Timeline;
using Itenso.TimePeriod;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Domain
{
    public static class TimelineUtils
    {
        public static IEnumerable<QueryTimelineRange<QueryPersonnelTimelineItem>> GeneratePersonnelTimeline(
            List<QueryPersonnelPosition> position,
            List<QueryPersonAbsenceBasic> absences,
            DateTime filterStart,
            DateTime filterEnd)
        {
            // Ensure utc dates
            if (filterStart.Kind != DateTimeKind.Utc)
                filterStart = DateTime.SpecifyKind(filterStart, DateTimeKind.Utc);

            if (filterEnd.Kind != DateTimeKind.Utc)
                filterEnd = DateTime.SpecifyKind(filterEnd, DateTimeKind.Utc);

            var timeline = GeneratePersonnelTimelineInternal(position, absences, filterStart, filterEnd).ToList();
            //FixOverlappingPeriods(timeline);
            return timeline;
        }
        private static IEnumerable<QueryTimelineRange<QueryPersonnelTimelineItem>> GeneratePersonnelTimelineInternal(
            List<QueryPersonnelPosition> positions,
            List<QueryPersonAbsenceBasic> absences,
            DateTime filterStart,
            DateTime filterEnd)
        {
            var timeline = new Timeline<QueryPersonnelTimelineItem>(x => x.AppliesFrom.Date, x => x.AppliesTo.Date);
            
            foreach (var p in positions)
            {
                timeline.Add(new QueryPersonnelTimelineItem
                {
                    Type = "PositionInstance",
                    Workload = p.Workload,
                    Id = p.PositionId,
                    Description = $"{p.Name}",
                    BasePosition = p.BasePosition,
                    Project = p.Project,
                    AppliesFrom = p.AppliesFrom,
                    AppliesTo = p.AppliesTo
                });
            }

            foreach(var a in absences)
            {
                timeline.Add(new QueryPersonnelTimelineItem
                {
                    Id = a.Id,
                    Type = "Absence",
                    Workload = a.AbsencePercentage,
                    Description = $"{a.Type}",
                    AppliesFrom = a.AppliesFrom.Date,
                    AppliesTo = a.AppliesTo.GetValueOrDefault(DateTime.MaxValue).Date
                });
            }

            var view = timeline.GetView(filterStart, filterEnd);
            return view.Segments.Select(x => new QueryTimelineRange<QueryPersonnelTimelineItem>(x.FromDate, x.ToDate)
            {
                Items = x.Items,
                Workload = x.Items.Sum(item => item.Workload ?? 0)
            });
        }

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
                timeline.Add(new QueryRequestsTimelineItem
                {
                    Workload = req.OrgPositionInstance?.Workload,
                    Id = req.RequestId.ToString(),
                    PositionName = req.OrgPosition?.Name,
                    ProjectName = req.Project.Name,
                    AppliesFrom = req.OrgPositionInstance!.AppliesFrom,
                    AppliesTo = req.OrgPositionInstance!.AppliesTo
                });
            }

            var view = timeline.GetView(filterStart, filterEnd);
            return view.Segments.Select(x => new QueryTimelineRange<QueryRequestsTimelineItem>(x.FromDate, x.ToDate)
            {
                Items = x.Items,
                Workload = x.Items.Sum(item => item.Workload ?? 0)
            });
        }

        public static IEnumerable<QueryTimelineRange<QueryTBNPositionTimelineItem>> GenerateTbnPositionsTimeline(
           IEnumerable<TbnPosition> tbnPositions,
           DateTime filterStart,
           DateTime filterEnd)
        {
            var segments = new List<QueryTimelineRange<QueryTBNPositionTimelineItem>>();
            var filterPeriod = new TimeRange(filterStart, filterEnd);

            var applicatablePositions = tbnPositions
                .Where(r => new TimeRange(r.AppliesTo, filterEnd).OverlapsWith(filterPeriod))
                .ToList();

            var timeline = new Timeline<TbnPosition>(x => x.AppliesFrom.Date, x => x.AppliesTo.Date);
            foreach(var position in applicatablePositions)
            {
                timeline.Add(position);
            }

            var view = timeline.GetView(filterStart, filterEnd);
            return view.Segments.Select(x => new QueryTimelineRange<QueryTBNPositionTimelineItem>(x.FromDate, x.ToDate)
            {
                Items = x.Items.Select(r => new QueryTBNPositionTimelineItem
                {
                    Workload = r.Workload,
                    Id = r.InstanceId.ToString(),
                    PositionName = r.Name,
                    ProjectId = r.ProjectId
                }).ToList(),
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

        private static void FixOverlappingPeriods<T>(List<QueryTimelineRange<T>> timeline)
        {
            // Tweek ranges where end date == next start date
            for (int i = 0; i < timeline.Count - 1; i++)
            {
                var now = timeline[i];
                var next = timeline[i + 1];

                if (now.AppliesTo == next.AppliesFrom)
                    now.AppliesTo = now.AppliesTo.Subtract(TimeSpan.FromDays(1));
            }
        }
    }
}
