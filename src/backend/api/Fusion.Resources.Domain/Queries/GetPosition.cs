using Fusion.ApiClients.Org;
using Fusion.Integration.Org;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetPosition : IRequest<ApiPositionV2?>
    {
        private readonly Guid posiitonId;

        public GetPosition(Guid posiitonId)
        {
            this.posiitonId = posiitonId;
        }

        public class Handler : IRequestHandler<GetPosition, ApiPositionV2?>
        {
            private readonly IProjectOrgResolver orgResolver;

            public Handler(IProjectOrgResolver orgResolver)
            {
                this.orgResolver = orgResolver;
            }

            public async Task<ApiPositionV2?> Handle(GetPosition request, CancellationToken cancellationToken)
            {
                return  await orgResolver.ResolvePositionAsync(request.posiitonId);
            }
        }
    }
}
