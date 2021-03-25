using Fusion.Resources.Database;
using Fusion.Resources.Domain;
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

                return await new RequestRouter(resourcesDb).Route(dbRequest, cancellationToken);
            }
        }
    }
}
