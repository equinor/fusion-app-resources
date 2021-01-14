using Fusion.Resources.Database;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain.Commands
{
    public class CreateProjectAllocationRequestCommand : TrackableRequest<QueryResourceAllocationRequest>
    {
        public CreateProjectAllocationRequestCommand(Guid requestId, Guid arg)
        {
            this.RequestId = requestId;
        }

        public Guid RequestId { get; set; }

        public class Handler : IRequestHandler<CreateProjectAllocationRequestCommand, QueryResourceAllocationRequest>
        {
            private readonly IOrgApiClient orgClient;
            private readonly ResourcesDbContext db;
            public Handler(IOrgApiClientFactory orgApiClientFactory, ResourcesDbContext db)
            {
                orgClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
                this.db = db;
            }

            public async Task<QueryResourceAllocationRequest> Handle(CreateProjectAllocationRequestCommand request, CancellationToken cancellationToken)
            {
                var row = await db.ResourceAllocationRequests.FirstOrDefaultAsync(c => c.Id == request.RequestId);
                return new QueryResourceAllocationRequest(row);
            }

        }

    }
}
