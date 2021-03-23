using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Queries
{
    internal class ResolveResponsibleDepartment : IRequest<string?>
    {
        public ResolveResponsibleDepartment(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public class Handler : IRequestHandler<ResolveResponsibleDepartment, string?>
        {
            private readonly ResourcesDbContext resourcesDb;

            public Handler(ResourcesDbContext resourcesDb)
            {
                this.resourcesDb = resourcesDb;
            }

            public async Task<string?> Handle(ResolveResponsibleDepartment request, CancellationToken cancellationToken)
            {
                var dbRequest = await resourcesDb.ResourceAllocationRequests.FirstAsync(r => r.Id == request.RequestId, cancellationToken);


                var matrix = await resourcesDb.ResponsibilityMatrices
                    .Include(m => m.Responsible)
                    .Include(m => m.Project)
                    .Select(m => new
                    {
                        Score = (m.Project!.Id == dbRequest.ProjectId ? 5 : 0)
                              + (m.Discipline == dbRequest.Discipline ? 2 : 0)
                              + (m.LocationId == dbRequest.OrgPositionInstance.LocationId ? 1 : 0),
                        Row = m
                    })
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefaultAsync(x => x.Score >= 5, cancellationToken);

                return matrix?.Row.Unit;
            }
        }
    }
}
