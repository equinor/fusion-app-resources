using Fusion.Resources.Domain.Timeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace Fusion.Resources.Domain.Tests
{
    public class Timeline
    {
        record TestItem(DateTime FromDate, DateTime ToDate, string Tag);


        public static IEnumerable<object[]> Data =>
        new List<object[]>
        {
            new object[] {
                new DateTime(2021, 04, 01),
                new DateTime(2021, 04, 30),

                new DateTime(2021, 04, 15),
                new DateTime(2021, 04, 16),
                true
            },

            new object[] {
                new DateTime(2021, 04, 01),
                new DateTime(2021, 04, 30),

                new DateTime(2021, 03, 15),
                new DateTime(2021, 04, 16),
                true
            },

            new object[] {
                new DateTime(2021, 04, 01),
                new DateTime(2021, 04, 30),

                new DateTime(2021, 03, 15),
                new DateTime(2021, 04, 01),
                true
            },
        };

        [Theory]
        [MemberData(nameof(Data))]
        public void SegmentsOverlap(DateTime fromDateA, DateTime toDateA, DateTime fromDateB, DateTime toDateB, bool shouldOverlap)
        {
            var a = new Segment<object> { FromDate = fromDateA, ToDate = toDateA };
            var b = new Segment<object> { FromDate = fromDateB, ToDate = toDateB };

            a.Overlaps(b).Should().Be(shouldOverlap);
        }

        /// <summary>
        /// ## Leave
        /// Positions:
        /// (2021-04-01, 2021-08-31)
        /// 
        /// Leave:
        /// (2021-04-15, 2021-04-30)
        /// 
        /// 
        /// 01/04		     14/4 15/04    30/04 01/05				                31/08
        /// .____________________.______________._______________________________________.
        /// :__________A_________:______B_______:____________________C__________________:
        /// 
        /// A: (2021-04-01, 2021-04-14) { position #1 }
        /// B: (2021-04-15, 2021-04-30) { position #1, leave #1 }
        /// C: (2021-05-01, 2021-08-31) { position #1 }
        /// </summary>
        [Fact]
        public void OverlappingMiddle()
        {
            var position1 = new TestItem(new DateTime(2021, 04, 01), new DateTime(2021, 08, 31), "Position #1");
            var leave1 = new TestItem(new DateTime(2021, 04, 15), new DateTime(2021, 04, 30), "Leave #1");

            var timeline = new Timeline<TestItem>(x => x.FromDate, x => x.ToDate);
            timeline.Add(position1);
            timeline.Add(leave1);

            var view = timeline.GetView(new DateTime(2021, 04, 01), new DateTime(2021, 08, 31));

            AreSame(view.Segments[0], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 04, 01),
                ToDate = new DateTime(2021, 04, 14),
                Items = new List<TestItem> { position1 }
            });
            AreSame(view.Segments[1], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 04, 15),
                ToDate = new DateTime(2021, 04, 30),
                Items = new List<TestItem> { position1, leave1 }
            });
            AreSame(view.Segments[2], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 05, 01),
                ToDate = new DateTime(2021, 08, 31),
                Items = new List<TestItem> { position1 }
            });
        }

        /// <summary>
        /// ## Leave overlapping positions
        /// 
        /// Positions:
        /// (2021-04-01, 2021-04-30)
        /// (2021-05-01, 2021-05-31)
        /// 
        /// Leave:
        /// (2021-04-15, 2021-05-15)
        /// 
        /// 01/04		     14/4  15/04 	30/04  01/05	15/05  16/05     	  31/08
        /// .____________________..______________..______________.._______________________.
        /// [__________A_________][______B_______][_______C______][____________D__________]
        /// 
        /// A: (2021-04-01, 2021-04-14) { position #1 }
        /// B: (2021-04-15, 2021-04-30) { position #1, leave #1 }
        /// C: (2021-05-01, 2021-04-15) { position #2, leave #1 } 
        /// D: (2021-05-16, 2021-08-31) { position #2 }
        /// </summary>
        [Fact]
        public void LeaveOverlappingPositions()
        {
            var position1 = new TestItem(new DateTime(2021, 04, 01), new DateTime(2021, 04, 30), "Position #1");
            var position2 = new TestItem(new DateTime(2021, 05, 01), new DateTime(2021, 05, 31), "Position #2");
            var leave = new TestItem(new DateTime(2021, 04, 15), new DateTime(2021, 05, 15), "Leave #1");

            var timeline = new Timeline<TestItem>(x => x.FromDate, x => x.ToDate);
            timeline.Add(position1);
            timeline.Add(position2);
            timeline.Add(leave);

            var view = timeline.GetView(new DateTime(2021, 04, 01), new DateTime(2021, 08, 31));

            AreSame(view.Segments[0], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 04, 01),
                ToDate = new DateTime(2021, 04, 14),
                Items = new List<TestItem> { position1 }
            });
            AreSame(view.Segments[1], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 04, 15),
                ToDate = new DateTime(2021, 04, 30),
                Items = new List<TestItem> { position1, leave }
            });
            AreSame(view.Segments[2], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 05, 01),
                ToDate = new DateTime(2021, 05, 15),
                Items = new List<TestItem> { position2, leave }
            });
            AreSame(view.Segments[3], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 05, 16),
                ToDate = new DateTime(2021, 05, 31),
                Items = new List<TestItem> { position2 }
            });
        }

        [Fact]
        public void LeaveOverlappingPositionsInvertedOrder()
        {
            var position1 = new TestItem(new DateTime(2021, 04, 01), new DateTime(2021, 04, 30), "Position #1");
            var position2 = new TestItem(new DateTime(2021, 05, 01), new DateTime(2021, 05, 31), "Position #2");
            var leave = new TestItem(new DateTime(2021, 04, 15), new DateTime(2021, 05, 15), "Leave #1");

            var timeline = new Timeline<TestItem>(x => x.FromDate, x => x.ToDate);
            timeline.Add(leave);
            timeline.Add(position2);
            timeline.Add(position1);

            var view = timeline.GetView(new DateTime(2021, 04, 01), new DateTime(2021, 08, 31));

            AreSame(view.Segments[0], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 04, 01),
                ToDate = new DateTime(2021, 04, 14),
                Items = new List<TestItem> { position1 }
            });
            AreSame(view.Segments[1], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 04, 15),
                ToDate = new DateTime(2021, 04, 30),
                Items = new List<TestItem> { position1, leave }
            });
            AreSame(view.Segments[2], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 05, 01),
                ToDate = new DateTime(2021, 05, 15),
                Items = new List<TestItem> { position2, leave }
            });
            AreSame(view.Segments[3], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 05, 16),
                ToDate = new DateTime(2021, 05, 31),
                Items = new List<TestItem> { position2 }
            });
        }


        /// <summary>
        /// ## Overlapping positions
        /// 
        /// Positions:
        /// (2021-04-01, 2021-04-30)
        /// (2021-04-30, 2021-05-31)
        /// (2021-06-01, 2021-06-30)
        /// 
        /// 01/04		30/04  05/01         31/05  01/06     03/06
        /// .________________..___..___________________..______________.
        /// [__________A_____][_B_][_________C_________][______D_______]
        /// 
        /// A: (2021-04-01, 2021-04-29) { position #1 }
        /// B: (2021-04-30, 2021-04-30) { position #1, position #2 }
        /// C: (2021-05-01, 2021-05-31) { position #2 }
        /// D: (2021-06-01, 2021-06-30) { position #3 }
        /// </summary>
        [Fact]
        public void OverlappingPositions()
        {
            var position1 = new TestItem(new DateTime(2021, 04, 01), new DateTime(2021, 04, 30), "position #1");
            var position2 = new TestItem(new DateTime(2021, 04, 30), new DateTime(2021, 05, 31), "position #2");
            var position3 = new TestItem(new DateTime(2021, 06, 01), new DateTime(2021, 06, 30), "position #3");

            var timeline = new Timeline<TestItem>(x => x.FromDate, x => x.ToDate);

            timeline.Add(position1);
            timeline.Add(position2);
            timeline.Add(position3);

            var view = timeline.GetView(new DateTime(2021, 04, 01), new DateTime(2021, 08, 31));


            AreSame(view.Segments[0], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 04, 01),
                ToDate = new DateTime(2021, 04, 29),
                Items = new List<TestItem> { position1 }
            });
            AreSame(view.Segments[1], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 04, 30),
                ToDate = new DateTime(2021, 04, 30),
                Items = new List<TestItem> { position1, position2 }
            });
            AreSame(view.Segments[2], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 05, 01),
                ToDate = new DateTime(2021, 05, 31),
                Items = new List<TestItem> { position2 }
            });
            AreSame(view.Segments[3], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 06, 01),
                ToDate = new DateTime(2021, 06, 30),
                Items = new List<TestItem> { position3 }
            });
        }

        [Fact]
        public void ViewShouldGroupItemsOutsideRange()
        {
            var position1 = new TestItem(new DateTime(2021, 04, 01), new DateTime(2021, 04, 30), "position #1");
            var position2 = new TestItem(new DateTime(2021, 04, 03), new DateTime(2021, 05, 31), "position #2");
            var position3 = new TestItem(new DateTime(2021, 05, 01), new DateTime(2021, 06, 30), "position #3");

            var timeline = new Timeline<TestItem>(x => x.FromDate, x => x.ToDate);

            timeline.Add(position1);
            timeline.Add(position2);
            timeline.Add(position3);

            var view = timeline.GetView(new DateTime(2021, 04, 04), new DateTime(2021, 05, 30));

            view.Segments.Should().HaveCount(2);
            AreSame(view.Segments[0], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 04, 04),
                ToDate = new DateTime(2021, 04, 30),
                Items = new List<TestItem> { position1, position2 }
            });
            AreSame(view.Segments[1], new Segment<TestItem>
            {
                FromDate = new DateTime(2021, 05, 01),
                ToDate = new DateTime(2021, 05, 30),
                Items = new List<TestItem> { position2, position3 }
            });
        }

        private static void AreSame(Segment<TestItem> a, Segment<TestItem> b)
        {
            a.FromDate.Should().Be(b.FromDate);
            a.ToDate.Should().Be(b.ToDate);

            a.Items.Should().BeEquivalentTo(b.Items);
        }
    }
}
