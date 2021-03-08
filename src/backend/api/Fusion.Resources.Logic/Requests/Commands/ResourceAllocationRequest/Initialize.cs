using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

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


                    var type = dbRequest.Type switch
                    {
                        DbInternalRequestType.Normal => InternalRequestType.Normal,
                        DbInternalRequestType.JointVenture => InternalRequestType.JointVenture,
                        DbInternalRequestType.Direct => InternalRequestType.Direct,

                        _ => throw new NotSupportedException($"Workflow init of type {dbRequest.Type} is not supported")
                    };

                    await mediator.Publish(new RequestInitialized(dbRequest.Id, type, request.Editor.Person));
                }


            }

        }

    }
}