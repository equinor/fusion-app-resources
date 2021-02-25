using FluentValidation;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Itenso.TimePeriod;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetDepartmentRequestsTimeline : IRequest<QueryDepartmentRequestsTimeline>
    {
        public GetDepartmentRequestsTimeline(string departmentString, DateTime timelineStart, DateTime timelineEnd, ODataQueryParams? query = null)
        {
            this.Query = query ?? new ODataQueryParams();
            this.DepartmentString = departmentString;
            this.TimelineStart = timelineStart;
            this.TimelineEnd = timelineEnd;
        }

        public string DepartmentString { get; private set; }
        private ODataQueryParams Query { get; set; }
        public DateTime? TimelineStart { get; set; }
        public DateTime? TimelineEnd { get; set; }

        public class Validator : AbstractValidator<GetDepartmentRequestsTimeline>
        {
            public Validator()
            {
                RuleFor(x => x.TimelineStart).NotNull();
                RuleFor(x => x.TimelineEnd).NotNull();
                RuleFor(x => x.TimelineEnd).GreaterThan(x => x.TimelineStart);
                RuleFor(x => x.DepartmentString).NotEmpty().WithMessage("Full department string must be provided");
            }
        }

        public class Handler : IRequestHandler<GetDepartmentRequestsTimeline, QueryDepartmentRequestsTimeline>
        {
            private readonly ResourcesDbContext db;
            private readonly IProjectOrgResolver orgResolver;

            public Handler(ResourcesDbContext db, IProjectOrgResolver orgResolver)
            {
                this.db = db;
                this.orgResolver = orgResolver;
            }

            //TODO
            public async Task<QueryDepartmentRequestsTimeline> Handle(GetDepartmentRequestsTimeline request, CancellationToken cancellationToken)
            {
                // get requests for department
                var query = db.ResourceAllocationRequests
                    .Include(r => r.OrgPositionInstance)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.ProposedPerson)
                    .Where(r => r.AssignedDepartment == request.DepartmentString)
                    .OrderBy(x => x.Id) // Should have consistent sorting due to OData criterias.
                    .ToList();

                var departmentRequests = new List<QueryResourceAllocationRequest>(query.Select(x => new QueryResourceAllocationRequest(x)));
                
                foreach (var departmentRequest in departmentRequests)
                {
                    if (departmentRequest.OrgPositionId != null)
                    {
                        var position = await orgResolver.ResolvePositionAsync(departmentRequest.OrgPositionId.Value);
                        if (position != null)
                        {
                            departmentRequest.WithResolvedOriginalPosition(position, departmentRequest.OrgPositionInstanceId);
                        }
                    }
                }
                // Timeline date input has been verified in controller
                var timeline = GenerateTimeline(request.TimelineStart!.Value, request.TimelineEnd!.Value, departmentRequests).OrderBy(p => p.AppliesFrom)
                            .Where(t => (t.AppliesTo - t.AppliesFrom).Days > 2) // We do not want 1 day intervals that occur due to from/to do not overlap
                            .ToList();

                var result = new QueryDepartmentRequestsTimeline
                {
                    Timeline = timeline,
                    Requests = departmentRequests
                };
                return result;
            }
        
            private IEnumerable<QueryTimelineRange<QueryDepartmentRequestsTimeline.DepartmentTimelineItem>> GenerateTimeline(
                DateTime filterStart,
                DateTime filterEnd, 
                List<QueryResourceAllocationRequest> requests)
            {
                // Ensure utc dates
                if (filterStart.Kind != DateTimeKind.Utc)
                    filterStart = DateTime.SpecifyKind(filterStart, DateTimeKind.Utc);

                if (filterEnd.Kind != DateTimeKind.Utc)
                    filterEnd = DateTime.SpecifyKind(filterEnd, DateTimeKind.Utc);

                //gather all dates from orgPositionInstances of each request
                var orgPositionInstances = requests.Select(r => r.OrgPositionInstance);
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
                    var timelineRange = new TimeRange(current, date);

                    var affectedItems = requests.Where(r =>
                    {
                        if (r.OrgPositionInstance == null) return false;
                        var requestTimeRange = new TimeRange(r.OrgPositionInstance.AppliesFrom.Date, r.OrgPositionInstance.AppliesTo.Date);
                        return requestTimeRange.OverlapsWith(timelineRange);
                    });
                    // create timelinerange with TimelineItems
                    yield return new QueryTimelineRange<QueryDepartmentRequestsTimeline.DepartmentTimelineItem>(timelineRange.Start, timelineRange.End)
                    {
                        Items = affectedItems.Select(r => new QueryDepartmentRequestsTimeline.DepartmentTimelineItem
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
        }
    }
}
