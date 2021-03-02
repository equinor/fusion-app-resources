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
                var timelineRange = new TimeRange(current, date);

                //bool overlap = a.start < b.end && b.start < a.end;

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
    }
}
