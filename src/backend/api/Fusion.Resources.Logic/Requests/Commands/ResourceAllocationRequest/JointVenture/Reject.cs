using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class JointVenture
        {
            public class Reject : TrackableRequest
            {
                public Reject(Guid requestId, string reason)
                {
                    RequestId = requestId;
                    Reason = reason;
                }

                public Guid RequestId { get; }
                public string Reason { get; }



                public class Handler : AsyncRequestHandler<Reject>
                {
                    private readonly IMediator mediator;

                    public Handler(IMediator mediator)
                    {
                        this.mediator = mediator;
                    }

                    protected override async Task Handle(Reject request, CancellationToken cancellationToken)
                    {
                        await mediator.Send(new SetState(request.RequestId, DbResourceAllocationRequestState.Rejected)
                            .WithReason(request.Reason));
                    }
                }
            }
        }
    }
}
