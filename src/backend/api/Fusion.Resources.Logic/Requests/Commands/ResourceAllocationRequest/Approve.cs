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
                    var dbRequest = await mediator.Send(new GetProjectResourceAllocationRequestItem(request.RequestId), CancellationToken.None);

                    switch (dbRequest.Type)
                    {
                        case QueryResourceAllocationRequest.QueryAllocationRequestType.Direct:
                            await mediator.Send(new Direct.SetState(request.RequestId, DbResourceAllocationRequestState.Accepted));
                            break;

                        case QueryResourceAllocationRequest.QueryAllocationRequestType.JointVenture:
                            await mediator.Send(new JointVenture.SetState(request.RequestId, DbResourceAllocationRequestState.Accepted));
                            break;

                        case QueryResourceAllocationRequest.QueryAllocationRequestType.Normal:
                            await mediator.Send(new Normal.SetState(request.RequestId, DbResourceAllocationRequestState.Accepted));
                            break;
                    }
                }
            }
        }
    }


}
