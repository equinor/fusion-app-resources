using Fusion.Resources.Database;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Org;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain.Queries
{
    public class GetProjectResourceAllocationRequests : IRequest<IEnumerable<QueryResourceAllocationRequest>>
    {

        public GetProjectResourceAllocationRequests(Guid projectId, ODataQueryParams? query = null)
        {
            this.Query = query ?? new ODataQueryParams();
            ProjectId = projectId;
        }

        public Guid ProjectId { get; }
        private ODataQueryParams Query { get; set; }
        private ExpandFields Expands { get; set; }

        [Flags]
        private enum ExpandFields
        {
            None = 0,
            OrgPosition = 1 << 0,
            All = OrgPosition
        }
        public class Handler : IRequestHandler<GetProjectResourceAllocationRequests, IEnumerable<QueryResourceAllocationRequest>>
        {
            private readonly ResourcesDbContext db;
            private readonly IProjectOrgResolver orgResolver;

            public Handler(ResourcesDbContext db, IProjectOrgResolver orgResolver)
            {
                this.db = db;
                this.orgResolver = orgResolver;
            }

            public async Task<IEnumerable<QueryResourceAllocationRequest>> Handle(GetProjectResourceAllocationRequests request, CancellationToken cancellationToken)
            {

                if (request.Query.ShoudExpand("OrgPosition"))
                    request.Expands |= ExpandFields.OrgPosition;


                var row = await db.ResourceAllocationRequests
                    .Include(r => r.OrgPositionInstance)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.ProposedPerson)
                    .Where(c => c.Project.OrgProjectId == request.ProjectId).ToListAsync();

                var requestItems = row.Select(x => new QueryResourceAllocationRequest(x)).ToList();

                if (!request.Expands.HasFlag(ExpandFields.OrgPosition))
                    return requestItems;


                // Expand original position.
                var resolvedOrgChartPositions =
                    (await orgResolver.ResolvePositionsAsync(row.Where(r => r.OriginalPositionId.HasValue)
                        .Select(r => r.OriginalPositionId!.Value))).ToList();
                
                // If none resolved, return.
                if (!resolvedOrgChartPositions.Any())
                    return requestItems;

                foreach (var queryResourceAllocationRequest in requestItems)
                {
                    if (queryResourceAllocationRequest.OrgPosition == null) continue;

                    var originalPosition = resolvedOrgChartPositions.FirstOrDefault(p =>
                        p.Id == queryResourceAllocationRequest.OrgPosition.Id);
                    if (originalPosition != null)
                        queryResourceAllocationRequest.WithResolvedOriginalPosition(queryResourceAllocationRequest.OrgPosition, queryResourceAllocationRequest.OrgPositionInstanceId);
                }

                return requestItems;
            }
        }
    }
}
