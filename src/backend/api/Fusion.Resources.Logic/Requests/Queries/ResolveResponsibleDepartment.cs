using Fusion.Integration.Org;
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
            private readonly IRequestRouter requestRouter;

            public Handler(ResourcesDbContext resourcesDb, IRequestRouter requestRouter)
            {
                this.resourcesDb = resourcesDb;
                this.requestRouter = requestRouter;
            }

            public async Task<string?> Handle(ResolveResponsibleDepartment request, CancellationToken cancellationToken)
            {
                var dbRequest = await resourcesDb.ResourceAllocationRequests.FirstAsync(r => r.Id == request.RequestId, cancellationToken);

                return await requestRouter.RouteAsync(dbRequest, cancellationToken);
            }
        }
    }
}
