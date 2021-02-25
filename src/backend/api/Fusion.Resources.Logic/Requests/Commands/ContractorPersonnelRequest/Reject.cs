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
                    var dbRequest = await mediator.Send(new GetContractPersonnelRequest(request.RequestId));

                    switch (dbRequest.State)
                    {
                        case DbRequestState.Created:
                            await mediator.Send(new SetState(request.RequestId, DbRequestState.RejectedByContractor).WithReason(request.Reason));
                            break;

                        case DbRequestState.SubmittedToCompany:
                            await mediator.Send(new SetState(request.RequestId, DbRequestState.RejectedByCompany).WithReason(request.Reason));
                            break;

                        default:
                            throw new NotSupportedException($"Invalid state change. Only supporting Created and SubmittedToCompany. State was:{dbRequest.State}");
                    }
                }
            }
        }
    }
}
