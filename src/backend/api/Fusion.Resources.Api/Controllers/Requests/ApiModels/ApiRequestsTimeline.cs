﻿using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiRequestsTimeline
    {
   
        public ApiRequestsTimeline(QueryRequestsTimeline qt, DateTime timelineStart, DateTime timelineEnd)
        {
            if (qt.Requests != null) Requests = qt.Requests.Select(r => new TimelineRequest(r, timelineStart, timelineEnd)).ToList();
            if (qt.Timeline != null) Timeline = qt.Timeline.Select(t => new RequestsTimelineRange(t)).ToList();
        }
        public List<TimelineRequest>? Requests { get; set; }
        public List<RequestsTimelineRange>? Timeline { get; set; }
        public class TimelineRequest
        {
            public TimelineRequest(QueryResourceAllocationRequest qr, DateTime timelineStart, DateTime timelineEnd) 
            {
                Id = qr.RequestId.ToString();
                if (qr.OrgPositionInstance != null)
                {
                    AppliesFrom = qr.OrgPositionInstance.AppliesFrom;
                    AppliesTo = qr.OrgPositionInstance.AppliesTo;
                    Workload = qr.OrgPositionInstance.Workload;
                    FilteredAppliesFrom = AppliesFrom < timelineStart ? timelineStart : AppliesFrom;
                    FilteredAppliesTo = AppliesTo > timelineEnd ? timelineEnd : AppliesTo; 
                }
                ProjectName = qr.Project.Name;
                PositionName = qr.OrgPosition != null ? qr.OrgPosition.Name : "";
                Discipline = qr.Discipline;
            }
            public string Id { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public DateTime FilteredAppliesFrom { get; set; }
            public DateTime FilteredAppliesTo { get; set; }
            public double? Workload { get; set; }
            public string ProjectName { get; set; }
            public string PositionName { get; set; }
            public string? Discipline { get; set; }
        }

        public class RequestsTimelineRange
        {
            public RequestsTimelineRange(QueryTimelineRange<QueryRequestsTimelineItem> ti)
            {
                AppliesFrom = ti.AppliesFrom;
                AppliesTo = ti.AppliesTo;
                Workload = ti.Workload;

                Items = ti.Items.Select(i => new RequestsTimelineItem(i)).ToList();
            }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public List<RequestsTimelineItem> Items { get; set; } = new List<RequestsTimelineItem>();
            public double Workload { get; set; } //sum of all requests within the window
        }

        public class RequestsTimelineItem
        {
            public RequestsTimelineItem(QueryRequestsTimelineItem item)
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