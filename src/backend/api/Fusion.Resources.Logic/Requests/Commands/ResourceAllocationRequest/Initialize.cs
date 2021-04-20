using Fusion.Resources.Database;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Notifications;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public class Initialize : TrackableRequest
        {

            public Initialize(Guid requestId)
            {
                RequestId = requestId;
            }

            public Guid RequestId { get; }



            public class Handler : AsyncRequestHandler<Initialize>
            {

                private readonly ResourcesDbContext resourcesDb;
                private readonly IMediator mediator;

                public Handler(ResourcesDbContext resourcesDb, IMediator mediator)
                {
                    this.resourcesDb = resourcesDb;
                    this.mediator = mediator;
                }

                protected override async Task Handle(Initialize request, CancellationToken cancellationToken)
                {
                    var dbRequest = await resourcesDb.ResourceAllocationRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId);
                    if (dbRequest is null)
                        throw new InvalidOperationException($"Could not locate request with id {request.RequestId}");

                    dbRequest.IsDraft = false;

                    await resourcesDb.SaveChangesAsync();

                    await mediator.Publish(new RequestInitialized(dbRequest.Id, dbRequest.Type, dbRequest.SubType, request.Editor.Person));

                    await mediator.Publish(new ResourceAllocationWorkflowChanged(dbRequest.Id));
                }


            }

        }

    }
}