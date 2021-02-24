using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ContractorPersonnelRequest
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
                    var dbRequest = await mediator.Send(new GetContractPersonnelRequest(request.RequestId));

                    switch (dbRequest.State)
                    {
                        case DbRequestState.Created:
                            await mediator.Send(new SetState(request.RequestId, DbRequestState.SubmittedToCompany));
                            break;

                        case DbRequestState.SubmittedToCompany:
                            await mediator.Send(new SetState(request.RequestId, DbRequestState.ApprovedByCompany));
                            break;

                        default:
                            throw new NotSupportedException($"Invalid state change. Only supporting Created and SubmittedToCompany. State was:{dbRequest.State}");
                    }
                }
            }
        }
    }


}
