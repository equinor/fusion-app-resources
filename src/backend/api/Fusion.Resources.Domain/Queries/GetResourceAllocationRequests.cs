using Fusion.Resources.Database;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Org;
using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain.Queries
{
    public class GetResourceAllocationRequests : IRequest<QueryPagedList<QueryResourceAllocationRequest>>
    {

        public GetResourceAllocationRequests(ODataQueryParams? query = null)
        {
            this.Query = query ?? new ODataQueryParams();

        }

        public GetResourceAllocationRequests WithProjectId(Guid projectId)
        {
            ProjectId = projectId;
            return this;
        }

        public GetResourceAllocationRequests WithAssignedDepartment(string departmentString)
        {
            DepartmentString = departmentString;
            return this;
        }

        public Guid? ProjectId { get; private set; }
        public string? DepartmentString { get; private set; }
        private ODataQueryParams Query { get; set; }
        private ExpandFields Expands { get; set; }

        [Flags]
        private enum ExpandFields
        {
            None = 0,
            OrgPosition = 1 << 0,
            All = OrgPosition
        }
        public class Handler : IRequestHandler<GetResourceAllocationRequests, QueryPagedList<QueryResourceAllocationRequest>>
        {
            private readonly ResourcesDbContext db;
            private readonly IProjectOrgResolver orgResolver;
            private const int DefaultPageSize = 100;

            public Handler(ResourcesDbContext db, IProjectOrgResolver orgResolver)
            {
                this.db = db;
                this.orgResolver = orgResolver;
            }

            public async Task<QueryPagedList<QueryResourceAllocationRequest>> Handle(GetResourceAllocationRequests request, CancellationToken cancellationToken)
            {

                if (request.Query.ShoudExpand("OrgPosition"))
                    request.Expands |= ExpandFields.OrgPosition;


                var query = db.ResourceAllocationRequests
                    .Include(r => r.OrgPositionInstance)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.ProposedPerson)
                    .OrderBy(x => x.Id) // Should have consistent sorting due to OData criterias.
                    .AsQueryable();

                if (request.Query.HasFilter)
                {
                    query = query.ApplyODataFilters(request.Query, m =>
                    {
                        m.MapField(nameof(QueryResourceAllocationRequest.AssignedDepartment), i => i.AssignedDepartment);
                        m.MapField(nameof(QueryResourceAllocationRequest.Discipline), i => i.Discipline);
                    });
                }

                if (request.ProjectId.HasValue)
                    query = query.Where(c => c.Project.OrgProjectId == request.ProjectId);

                if (request.DepartmentString != null)
                    query = query.Where(c => c.AssignedDepartment == request.DepartmentString);

                var pagedQuery = await QueryPagedList<DbResourceAllocationRequest>.ToPagedListAsync(query,
                    request.Query.Skip.GetValueOrDefault(1), request.Query.Top.GetValueOrDefault(DefaultPageSize));

                var requestItems = new QueryPagedList<QueryResourceAllocationRequest>(pagedQuery.Select(x => new QueryResourceAllocationRequest(x)), pagedQuery.TotalCount,
                    pagedQuery.CurrentPage, pagedQuery.PageSize);

                if (!request.Expands.HasFlag(ExpandFields.OrgPosition))
                    return requestItems;


                // Expand original position.
                var resolvedOrgChartPositions =
                    (await orgResolver.ResolvePositionsAsync(requestItems.Where(r => r.OrgPositionId.HasValue)
                        .Select(r => r.OrgPositionId!.Value))).ToList();

                // If none resolved, return.
                if (!resolvedOrgChartPositions.Any())
                    return requestItems;

                foreach (var queryResourceAllocationRequest in requestItems)
                {
                    if (queryResourceAllocationRequest.OrgPositionId == null) continue;

                    var position = resolvedOrgChartPositions.FirstOrDefault(p =>
                        p.Id == queryResourceAllocationRequest.OrgPositionId);

                    if (position != null)
                        queryResourceAllocationRequest.WithResolvedOriginalPosition(position, queryResourceAllocationRequest.OrgPositionInstanceId);
                }

                return requestItems;
            }
        }
    }
}
