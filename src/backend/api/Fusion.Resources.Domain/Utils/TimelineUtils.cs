using Itenso.TimePeriod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


            // Gather all dates 
            var dates = position.SelectMany(p => new[] { (DateTime?)p.AppliesFrom.Date, (DateTime?)p.AppliesTo.Date })
                .Union(absences.SelectMany(a => new[] { a.AppliesFrom.Date, a.AppliesTo?.Date }))
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (!dates.Any())
                yield break;

            var validDates = dates.Where(d => d > filterStart && d < filterEnd).ToList();

            validDates.Insert(0, filterStart);
            validDates.Add(filterEnd);

            var current = validDates.First();
            foreach (var date in validDates.Skip(1))
            {
                var timelineRange = new TimeRange(current, date.AddSeconds(-1));

                var affectedItems = position.Where(p =>
                {
                    var posTimeRange = new TimeRange(p.AppliesFrom.Date, p.AppliesTo.Date);
                    return posTimeRange.OverlapsWith(timelineRange);
                });
                var relevantAbsence = absences.Where(p => p.AppliesTo.HasValue && timelineRange.OverlapsWith(new TimeRange(p.AppliesFrom.Date, p.AppliesTo!.Value.Date)));

                yield return new QueryTimelineRange<QueryPersonnelTimelineItem>(current, date)
                {
                    Items = affectedItems.Select(p => new QueryPersonnelTimelineItem()
                    {
                        Type = "PositionInstance",
                        Workload = p.Workload,
                        Id = p.PositionId,
                        Description = $"{p.Name}",
                        BasePosition = p.BasePosition,
                        Project = p.Project
                    }).Union(relevantAbsence.Select(a => new QueryPersonnelTimelineItem()
                    {
                        Id = a.Id,
                        Type = "Absence",
                        Workload = a.AbsencePercentage,
                        Description = $"{a.Type}"
                    }))
                    .ToList(),
                    Workload = affectedItems.Sum(p => p.Workload) + relevantAbsence.Where(a => a.AbsencePercentage.HasValue).Sum(a => a.AbsencePercentage!.Value)
                };

                current = date;
            }
        }

        public static IEnumerable<QueryTimelineRange<QueryRequestsTimelineItem>> GenerateRequestsTimeline(
            List<QueryResourceAllocationRequest> requests,
            DateTime filterStart,
            DateTime filterEnd)
        {
            //gather all dates from orgPositionInstances of each request
            var orgPositionInstances = requests.Select(r => r.OrgPositionInstance)
                .Where(p => p != null)
                .Cast<ApiClients.Org.ApiPositionInstanceV2>();

            var dates = orgPositionInstances.SelectMany(p => new[] { (DateTime?)p.AppliesFrom.Date, (DateTime?)p.AppliesTo.Date })
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (!dates.Any())
                yield break;

            // choose dates within filter range
            var validDates = dates.Where(d => d > filterStart && d < filterEnd).ToList();

            validDates.Insert(0, filterStart);
            validDates.Add(filterEnd);

            var current = validDates.First();

            //create timeline
            foreach (var date in validDates.Skip(1))
            {
                var end = (date == filterEnd) ? date : date.AddSeconds(-1);
                var timelineRange = new TimeRange(current, end);

                var affectedItems = FilterRequests(requests, timelineRange);
                // create timelinerange with TimelineItems
                yield return new QueryTimelineRange<QueryRequestsTimelineItem>(timelineRange.Start, timelineRange.End)
                {
                    Items = affectedItems.Select(r => new QueryRequestsTimelineItem
                    {
                        Workload = r.OrgPositionInstance?.Workload,
                        Id = r.RequestId.ToString(),
                        PositionName = r.OrgPosition?.Name,
                        ProjectName = r.Project.Name
                    })
                    .ToList(),
                    Workload = affectedItems.Sum(r => r.OrgPositionInstance?.Workload ?? 0)
                };
                current = date;
            }
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
