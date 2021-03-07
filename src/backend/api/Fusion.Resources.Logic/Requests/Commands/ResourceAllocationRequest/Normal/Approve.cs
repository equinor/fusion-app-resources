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
using Fusion.Resources.Logic.Workflows;

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
                        //RuleFor(x => x.RequestId).MustAsync(async (id, cancel) =>
                        //{
                        //    return await db.ResourceAllocationRequests.AnyAsync(y => y.Id == id && y.Type == DbResourceAllocationRequest.DbAllocationRequestType.Normal);
                        //}).WithMessage($"Request of type: '{DbResourceAllocationRequest.DbAllocationRequestType.Normal}' must exist to be able to approve.");
                    }
                }

                public class Handler : AsyncRequestHandler<Approve>
                {
                    private readonly ResourcesDbContext dbContext;
                    private readonly IMediator mediator;

                    public Handler(ResourcesDbContext dbContext, IMediator mediator)
                    {
                        this.dbContext = dbContext;
                        this.mediator = mediator;
                    }

                    protected override async Task Handle(Approve request, CancellationToken cancellationToken)
                    {
                        var dbRequest = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId);
                        if (dbRequest is null)
                            throw new InvalidOperationException("Could not locate request");

                        var dbWorkflow = await mediator.GetRequestWorkflowAsync(dbRequest.Id);
                        var workflow = new InternalRequestNormalWorkflowV1(dbWorkflow);

                        if (dbRequest.State.State is null)
                            throw new InvalidOperationException("Workflow has not been initialized");

                        switch (dbRequest.State.State)
                        {
                            case InternalRequestNormalWorkflowV1.CREATED:
                                workflow.Proposed(request.Editor.Person);
                                break;

                            case InternalRequestNormalWorkflowV1.PROPOSAL:
                                workflow.Approved(request.Editor.Person);
                                break;

                            case InternalRequestNormalWorkflowV1.APPROVAL:
                                break;
                        }

                        dbRequest.State.State = workflow.GetCurrent().Id;
                        workflow.SaveChanges();

                        await dbContext.SaveChangesAsync(cancellationToken);

                        var notification = new RequestStateChanged(dbRequest.Id, InternalRequestType.Normal, workflow.GetCurrent().PreviousStepId, workflow.GetCurrent().Id);
                        await mediator.Publish(notification, cancellationToken);

                        //switch (dbRequest!.State)
                        //{
                        //    case  DbResourceAllocationRequestState.Created:
                        //        await mediator.Send(new SetState(request.RequestId, DbResourceAllocationRequestState.Proposed));
                        //        break;
                        //    case DbResourceAllocationRequestState.Proposed:
                        //        await mediator.Send(new SetState(request.RequestId, DbResourceAllocationRequestState.Accepted));
                        //        break;
                        //    default:
                        //        throw new NotSupportedException($"Invalid state change. Only supporting Created and Proposed. State was:{dbRequest.State}");
                        //}
                    }
                }
            }
        }
    }
}
