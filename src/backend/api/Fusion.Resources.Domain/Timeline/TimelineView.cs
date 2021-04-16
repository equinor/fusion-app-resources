using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Domain.Timeline
{
    public class TimelineView<T>
    {
        public DateTime FromDate { get; }
        public DateTime ToDate { get; }
        public TimelineView(DateTime fromDate, DateTime toDate)
        {
            FromDate = fromDate;
            ToDate = toDate;
        }

        public List<T> Items { get; } = new List<T>();
        public List<Segment<T>> Segments { get; } = new List<Segment<T>>();

        public static TimelineView<T> Create(DateTime fromDate, DateTime toDate, IList<Segment<T>> segments)
        {
            var view = new TimelineView<T>(fromDate, toDate);

            bool hasTruncatedFirst = false;
            bool hasTruncatedLast = false;

            var first = new Segment<T>
            {
                FromDate = fromDate,
                ToDate = DateTime.MinValue
            };
            var last = new Segment<T>
            {
                FromDate = DateTime.MaxValue,
                ToDate = toDate
            };

            foreach(var segment in segments)
            {
                if(segment.FromDate < fromDate && segment.ToDate >= fromDate)
                {
                    hasTruncatedFirst = true;
                    first.ToDate = Max(first.ToDate, segment.ToDate);
                    first.Items = first.Items.Union(segment.Items).ToList();
                }
                else if(segment.ToDate > toDate && segment.FromDate <= toDate)
                {
                    hasTruncatedLast = true;
                    last.FromDate = Min(last.FromDate, segment.FromDate);
                    last.Items = last.Items.Union(segment.Items).ToList();
                }
                else if(segment.FromDate >= fromDate && segment.ToDate <= toDate)
                {
                    view.Segments.Add(segment);
                }
            }

            if(hasTruncatedFirst) view.Segments.Insert(0, first);
            if(hasTruncatedLast) view.Segments.Add(last);

            return view;
        }

        private static DateTime Min(DateTime x, DateTime y) => x < y ? x : y;
        private static DateTime Max(DateTime x, DateTime y) => x > y ? x : y;

    }
}
