using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class Normal
        {
            internal class Initialize : TrackableRequest
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

                    private DbResourceAllocationRequest dbItem = null!;

                    private async Task ValidateAsync(Initialize request)
                    {
                        dbItem = await resourcesDb.ResourceAllocationRequests
                            .Include(r => r.Project)
                            .FirstOrDefaultAsync(r => r.Id == request.RequestId);


                        if (dbItem == null)
                            throw new InvalidOperationException($"Cannot resolve request {request.RequestId}");

                    }

                    protected override async Task Handle(Initialize request, CancellationToken cancellationToken)
                    {
                        await ValidateAsync(request);
                        await mediator.Publish(new InternalRequestCreated(request.RequestId));

                    }

                }
            }
        }


    }
}