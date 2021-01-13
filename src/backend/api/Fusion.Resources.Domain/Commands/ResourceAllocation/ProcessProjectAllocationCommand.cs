using Fusion.Resources.Database;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class ProcessProjectAllocationCommand : TrackableRequest<QueryProjectAllocationRequest>
    {
        public ProcessProjectAllocationCommand(Guid id, Guid arg)
        {
            
        }

        public class Handler : IRequestHandler<ProcessProjectAllocationCommand, QueryProjectAllocationRequest>
        {
            private readonly IOrgApiClient orgClient;
            private readonly ResourcesDbContext resourcesDb;
            public Handler(IOrgApiClientFactory orgApiClientFactory, ResourcesDbContext resourcesDb)
            {
                orgClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
                this.resourcesDb = resourcesDb;
            }

            public async Task<QueryProjectAllocationRequest> Handle(ProcessProjectAllocationCommand request, CancellationToken cancellationToken)
            {
              return new QueryProjectAllocationRequest();
            }

        }

    }

    public class QueryProjectAllocationRequest
    {
        public QueryProjectAllocationRequest()
        {
            
        }

        public Guid RequestId { get; set; }
    }
    
}
