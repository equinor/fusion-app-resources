using Fusion.Resources.Database;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Commands;

namespace Fusion.Resources.Domain.Queries
{
    public class GetProjectAllocationRequest : IRequest<QueryProjectAllocationRequest>
    {
        public GetProjectAllocationRequest(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public class Handler : IRequestHandler<GetProjectAllocationRequest, QueryProjectAllocationRequest?>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QueryProjectAllocationRequest?> Handle(GetProjectAllocationRequest request, CancellationToken cancellationToken)
            {
                return new QueryProjectAllocationRequest();
            }
        }
    }
}
