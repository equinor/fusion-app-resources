using System;
using System.Collections.Generic;

namespace Fusion.Resources.Domain.Timeline
{
    public class Segment<T>
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<T> Items { get; set; } = new List<T>();

        internal bool Combine(Segment<T> other, out Segment<T>? split)
        {
            split = null;

            if (!Overlaps(other)) return false;
            if (other.FromDate > ToDate) return other.Combine(this, out split);

            var splitDate = other.FromDate;

            if (other.ToDate < ToDate)
            {
                other.Items.AddRange(Items);

                var end = new Segment<T>
                {
                    FromDate = other.ToDate.AddDays(1),
                    ToDate = ToDate
                };
                end.Items.AddRange(Items);

                split = end;
            }
            else if (other.ToDate == ToDate)
            {
                other.Items.AddRange(Items);
            }
            else if (other.ToDate > ToDate)
            {
                var middle = new Segment<T>
                {
                    FromDate = other.FromDate,
                    ToDate = ToDate
                };
                middle.Items.AddRange(Items);
                middle.Items.AddRange(other.Items);

                other.FromDate = ToDate.AddDays(1);
                split = middle;
            }

            ToDate = splitDate.AddDays(-1);

            return true;
        }

        public bool Overlaps(Segment<T> other)
        {
            return
                (other.FromDate >= FromDate && other.FromDate <= ToDate) ||
                (FromDate >= other.FromDate && FromDate <= other.ToDate);
        }

        public bool IsValid => FromDate <= ToDate;
    }
}
