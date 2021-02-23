using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class Normal
        {
            public class Approve : TrackableRequest
            {
                public Approve(Guid requestId)
                {
                    RequestId = requestId;
                }
                public Guid RequestId { get; }
                
                public class Handler : AsyncRequestHandler<Approve>
                {
                    private readonly IMediator mediator;

                    public Handler(IMediator mediator)
                    {
                        this.mediator = mediator;
                    }

                    protected override async Task Handle(Approve request, CancellationToken cancellationToken)
                    {
                        var dbRequest = await mediator.Send(new GetResourceAllocationRequestItem(request.RequestId),
                            CancellationToken.None);


                        if (dbRequest!.Type != QueryResourceAllocationRequest.QueryAllocationRequestType.Normal)
                            throw new NotSupportedException($"{dbRequest.Type} not supported. Should be {QueryResourceAllocationRequest.QueryAllocationRequestType.Normal}");

                        switch (dbRequest.State)
                        {
                            case DbResourceAllocationRequestState.Created:
                                await mediator.Send(new SetState(request.RequestId, DbResourceAllocationRequestState.Proposed));
                                break;
                            case DbResourceAllocationRequestState.Proposed:
                                await mediator.Send(new SetState(request.RequestId, DbResourceAllocationRequestState.Accepted));
                                break;
                            default:
                                throw new NotSupportedException($"Invalid state change. Only supporting Created and Proposed. State was:{dbRequest.State}");
                        }
                    }
                }
            }
        }
    }
}
