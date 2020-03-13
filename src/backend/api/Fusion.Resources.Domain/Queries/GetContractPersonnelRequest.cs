using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{


    public class GetContractPersonnelRequest : IRequest<QueryPersonnelRequest>
    {
        public GetContractPersonnelRequest(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }


        public class Handler : IRequestHandler<GetContractPersonnelRequest, QueryPersonnelRequest>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext resourcesDb, IProjectOrgResolver orgResolver, IMediator mediator)
            {
                this.resourcesDb = resourcesDb;
                this.orgResolver = orgResolver;
                this.mediator = mediator;
            }

            public async Task<QueryPersonnelRequest> Handle(GetContractPersonnelRequest request, CancellationToken cancellationToken)
            {
                var dbRequest = await resourcesDb.ContractorRequests
                    .Include(r => r.Person).ThenInclude(p => p.Person).ThenInclude(p => p.Disciplines)
                    .Include(r => r.Person).ThenInclude(p => p.CreatedBy)
                    .Include(r => r.Person).ThenInclude(p => p.UpdatedBy)
                    .Include(r => r.Person).ThenInclude(p => p.Project)
                    .Include(r => r.Person).ThenInclude(p => p.Contract)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.Contract)
                    .FirstOrDefaultAsync(r => r.Id == request.RequestId);

                var basePosition = await orgResolver.ResolveBasePositionAsync(dbRequest.Position.BasePositionId);

                var position = new QueryPositionRequest(dbRequest.Position)
                    .WithResolvedBasePosition(basePosition);

                var workflow = await mediator.Send(new GetRequestWorkflow(request.RequestId));

                var returnItem = new QueryPersonnelRequest(dbRequest, position, workflow);
                return returnItem;
            }
        }
    }
}

