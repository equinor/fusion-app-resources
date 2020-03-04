using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

            public Handler(ResourcesDbContext resourcesDb, IProjectOrgResolver orgResolver)
            {
                this.resourcesDb = resourcesDb;
                this.orgResolver = orgResolver;
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

                var returnItem = new QueryPersonnelRequest(dbRequest, position);
                return returnItem;
            }
        }
    }
}

