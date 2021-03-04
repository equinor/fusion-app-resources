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
    public class GetDepartmentRequestsTimeline : IRequest<QueryRequestsTimeline>
    {
        public GetDepartmentRequestsTimeline(string departmentString, DateTime timelineStart, DateTime timelineEnd, ODataQueryParams? queryParams = null)
        {
            this.QueryParams = queryParams;
            this.DepartmentString = departmentString;
            this.TimelineStart = timelineStart;
            this.TimelineEnd = timelineEnd;
        }

        public string DepartmentString { get; private set; }
        public ODataQueryParams? QueryParams { get; set; }
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

        public class Handler : IRequestHandler<GetDepartmentRequestsTimeline, QueryRequestsTimeline>
        {
            private readonly ResourcesDbContext db;
            private readonly IProjectOrgResolver orgResolver;

            public Handler(ResourcesDbContext db, IProjectOrgResolver orgResolver)
            {
                this.db = db;
                this.orgResolver = orgResolver;
            }

            public async Task<QueryRequestsTimeline> Handle(GetDepartmentRequestsTimeline request, CancellationToken cancellationToken)
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
                var filterStart = request.TimelineStart!.Value;
                var filterEnd = request.TimelineEnd!.Value;

                // Ensure utc dates
                if (filterStart.Kind != DateTimeKind.Utc)
                    filterStart = DateTime.SpecifyKind(filterStart, DateTimeKind.Utc);

                if (filterEnd.Kind != DateTimeKind.Utc)
                    filterEnd = DateTime.SpecifyKind(filterEnd, DateTimeKind.Utc);

                var relevantRequests = TimelineUtils.FilterRequests(departmentRequests, new TimeRange(filterStart, filterEnd)).ToList();
                
                var timeline = TimelineUtils.GenerateRequestsTimeline(relevantRequests, filterStart, filterEnd).OrderBy(p => p.AppliesFrom)
                            .Where(t => (t.AppliesTo - t.AppliesFrom).Days > 2) // We do not want 1 day intervals that occur due to from/to do not overlap
                            .ToList();

                var result = new QueryRequestsTimeline
                {
                    Timeline = timeline,
                    Requests = relevantRequests
                };
                return result;
            }
        }
    }
}
