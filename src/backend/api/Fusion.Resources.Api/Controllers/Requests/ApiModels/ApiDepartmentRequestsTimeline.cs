using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiDepartmentRequestsTimeline
    {
   
        public ApiDepartmentRequestsTimeline(QueryDepartmentRequestsTimeline qt)
        {
            if (qt.Requests != null) Requests = qt.Requests.Select(r => new SimpleRequest(r)).ToList();
            if (qt.Timeline != null) Timeline = qt.Timeline.Select(t => new DepartmentRequestsTimelineRange(t)).ToList();
        }
        public List<SimpleRequest>? Requests { get; set; }
        public List<DepartmentRequestsTimelineRange>? Timeline { get; set; }
        public class SimpleRequest
        {
            public SimpleRequest(QueryResourceAllocationRequest qr) 
            {
                Id = qr.RequestId.ToString();
                AppliesFrom = qr.OrgPositionInstance.AppliesFrom;
                AppliesTo = qr.OrgPositionInstance.AppliesTo;
                Workload = qr.OrgPositionInstance.Workload;
                ProjectName = qr.Project.Name;
                PositionName = qr.OrgPosition.Name;
            }
            public string Id { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public double? Workload { get; set; }
            public string ProjectName { get; set; }
            public string PositionName { get; set; }
        }

        public class DepartmentRequestsTimelineRange
        {
            public DepartmentRequestsTimelineRange(QueryTimelineRange<QueryDepartmentRequestsTimeline.DepartmentTimelineItem> ti)
            {
                AppliesFrom = ti.AppliesFrom;
                AppliesTo = ti.AppliesTo;
                Workload = ti.Workload;

                Items = ti.Items.Select(i => new RequestTimelineItem(i)).ToList();
            }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public List<RequestTimelineItem> Items { get; set; } = new List<RequestTimelineItem>();
            public double? Workload { get; set; } //sum of all requests within the window
        }

        public class RequestTimelineItem
        {
            public RequestTimelineItem(QueryDepartmentRequestsTimeline.DepartmentTimelineItem item)
            {
                Id = item.Id;
                Workload = item.Workload;
                PositionName = item.PositionName;
                ProjectName = item.ProjectName;
            }
            public string Id { get; set; } // ref to item in requests collection
            public string? PositionName { get; set; }
            public string? ProjectName { get; set; }
            public double? Workload { get; set; }
        }
    }
}