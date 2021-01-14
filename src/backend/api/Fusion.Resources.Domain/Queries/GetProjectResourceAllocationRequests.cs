using Fusion.Resources.Database;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Commands;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain.Queries
{
    public class GetProjectResourceAllocationRequests : IRequest<IEnumerable<QueryResourceAllocationRequest>>
    {
        public GetProjectResourceAllocationRequests(Guid projectId)
        {
            ProjectId = projectId;
        }

        public Guid ProjectId { get; }

        public class Handler : IRequestHandler<GetProjectResourceAllocationRequests, IEnumerable<QueryResourceAllocationRequest>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<IEnumerable<QueryResourceAllocationRequest>> Handle(GetProjectResourceAllocationRequests request, CancellationToken cancellationToken)
            {
                var row = await db.ResourceAllocationRequests
                    .Include(r => r.ResourceAllocationOrgPositionInstance)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.ProposedPerson)
                    .Where(c => c.Project.OrgProjectId == request.ProjectId).ToListAsync();
                return row.Select(x => new QueryResourceAllocationRequest(x));
            }
        }
    }
}
