using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Domain.Timeline
{

    public class Timeline<T>
    {
        private readonly Func<T, DateTime> getFromDate;
        private readonly Func<T, DateTime> getToDate;

        private readonly List<T> items;

        public Timeline(Func<T, DateTime> getFromDate, Func<T, DateTime> getToDate)
        {
            this.getFromDate = getFromDate;
            this.getToDate = getToDate;

            items = new List<T>();
        }

        public void Add(T item) => items.Add(item);

        private static List<Segment<T>> GetOverlappingSegments(List<Segment<T>> segments, Segment<T> newSegment)
        {
            return segments
                .Where(x => x.Overlaps(newSegment))
                .ToList();
        }

        public TimelineView<T> GetView(DateTime fromDate, DateTime toDate)
        {
            var segments = new List<Segment<T>>();
            foreach (var item in items.OrderBy(x => getFromDate(x)))
            {
                var newSegment = new Segment<T>
                {
                    FromDate = getFromDate(item),
                    ToDate = getToDate(item),
                    Items = new List<T> { item }
                };

                var overlappingSegments = GetOverlappingSegments(segments, newSegment);
                var splits = new List<Segment<T>>();

                foreach (var overlap in overlappingSegments)
                {
                    if (overlap.Combine(newSegment, out Segment<T>? split) && split is not null)
                        splits.Add(split);
                }

                segments.AddRange(splits);
                segments.Add(newSegment);

                segments.RemoveAll(x => !x.IsValid);
                segments.Sort((x, y) => x.FromDate.CompareTo(y.FromDate));
            }
            return TimelineView<T>.Create(fromDate, toDate, segments);
        }
    }
}
