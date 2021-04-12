using Fusion.Resources.Domain.Queries;
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
            List<QueryPersonnelPosition> position,
            List<QueryPersonAbsenceBasic> absences,
            DateTime filterStart,
            DateTime filterEnd)
        {
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
                var timelineRange = new TimeRange(current, date);

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
            var segments = new List<QueryTimelineRange<QueryRequestsTimelineItem>>();
            var filterPeriod = new TimeRange(filterStart, filterEnd);

            var applicableRequests = requests
                .Where(r => r.OrgPositionInstance is not null
                    && new TimeRange(r.OrgPositionInstance!.AppliesTo, r.OrgPositionInstance!.AppliesFrom).OverlapsWith(filterPeriod))
                .OrderBy(r => r.OrgPositionInstance!.AppliesFrom)
                .ThenBy(r => r.OrgPositionInstance!.AppliesTo)
                .ToList();

            var keyDates = new HashSet<DateTime>
            {
                filterStart,
                filterEnd
            };

            foreach (var req in applicableRequests)
            {
                var startDate = req.OrgPositionInstance!.AppliesFrom.Date;
                var endDate = req.OrgPositionInstance!.AppliesTo.Date;

                if (endDate <= filterEnd && !keyDates.Contains(endDate))
                {
                    keyDates.Add(endDate);
                }

                if (startDate >= filterStart && !keyDates.Contains(startDate))
                {
                    keyDates.Add(startDate);
                }
            }

            var orderedKeyDates = keyDates.OrderBy(d => d);

            var timeline = orderedKeyDates.Zip(orderedKeyDates.Skip(1), (start, end) =>
            {
                var range = new TimeRange(start, end, isReadOnly: true);
                var requestsInRange = applicableRequests.Where(req =>
                    new TimeRange(req.OrgPositionInstance!.AppliesFrom.Date, req.OrgPositionInstance!.AppliesTo.Date).OverlapsWith(range)
                );

                return new QueryTimelineRange<QueryRequestsTimelineItem>(start, end)
                {
                    Items = requestsInRange.Select(r => new QueryRequestsTimelineItem
                    {
                        Workload = r.OrgPositionInstance?.Workload,
                        Id = r.RequestId.ToString(),
                        PositionName = r.OrgPosition?.Name,
                        ProjectName = r.Project.Name
                    })
                    .ToList(),
                    Workload = requestsInRange.Sum(r => r.OrgPositionInstance?.Workload ?? 0)
                };
            })
            .Where(range => range.Items.Any())
            .ToList();

            //FixOverlappingPeriods(timeline);

            return timeline;
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
                .OrderBy(r => r.AppliesFrom)
                .ThenBy(r => r.AppliesTo)
                .ToList();

            var keyDates = new HashSet<DateTime>
            {
                filterStart,
                filterEnd
            };

            foreach (var req in applicatablePositions)
            {
                var startDate = req.AppliesFrom.Date;
                var endDate = req.AppliesTo.Date;

                if (endDate <= filterEnd && !keyDates.Contains(endDate))
                {
                    keyDates.Add(endDate);
                }

                if (startDate >= filterStart && !keyDates.Contains(startDate))
                {
                    keyDates.Add(startDate);
                }
            }

            var orderedKeyDates = keyDates.OrderBy(d => d);

            var timeline = orderedKeyDates.Zip(orderedKeyDates.Skip(1), (start, end) =>
            {
                var range = new TimeRange(start, end, isReadOnly: true);
                var requestsInRange = applicatablePositions.Where(req =>
                    new TimeRange(req.AppliesFrom.Date, req.AppliesTo.Date).OverlapsWith(range)
                );

                return new QueryTimelineRange<QueryTBNPositionTimelineItem>(start, end)
                {
                    Items = requestsInRange.Select(r => new QueryTBNPositionTimelineItem
                    {
                        Workload = r.Workload,
                        Id = r.InstanceId.ToString(),
                        PositionName = r.Name,
                        ProjectId = r.ProjectId
                    })
                    .ToList(),
                    Workload = requestsInRange.Sum(r => r.Workload ?? 0)
                };
            })
            .Where(range => range.Items.Any())
            .ToList();

            //FixOverlappingPeriods(timeline);

            return timeline;
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
