using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Fusion.Resources.Database;
using Fusion.Resources.Domain;
using Microsoft.EntityFrameworkCore;

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
                public class Validator : AbstractValidator<Approve>
                {
                    public Validator(ResourcesDbContext db)
                    {
                        RuleFor(x => x.RequestId).MustAsync(async (id, cancel) =>
                        {
                            return await db.ResourceAllocationRequests.AnyAsync(y => y.Id == id && y.Type == DbResourceAllocationRequest.DbAllocationRequestType.Normal);
                        }).WithMessage($"Request of type: '{DbResourceAllocationRequest.DbAllocationRequestType.Normal}' must exist to be able to approve.");
                    }
                }

                public class Handler : AsyncRequestHandler<Approve>
                {
                    private readonly IMediator mediator;

                    public Handler(IMediator mediator)
                    {
                        this.mediator = mediator;
                    }

                    protected override async Task Handle(Approve request, CancellationToken cancellationToken)
                    {
                        var dbRequest = await mediator.Send(new GetResourceAllocationRequestItem(request.RequestId), CancellationToken.None);
                        
                        switch (dbRequest!.State)
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
