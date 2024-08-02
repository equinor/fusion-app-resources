using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Notifications.InternalRequests;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

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


            public class Handler : IRequestHandler<Approve>
            {
                private readonly ResourcesDbContext dbContext;
                private readonly IMediator mediator;

                public Handler(ResourcesDbContext dbContext, IMediator mediator)
                {
                    this.dbContext = dbContext;
                    this.mediator = mediator;
                }

                public async Task Handle(Approve request, CancellationToken cancellationToken)
                {
                    var dbRequest =
                        await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId,
                            cancellationToken);
                    if (dbRequest is null)
                        throw new InvalidOperationException("Could not locate request");

                    var dbWorkflow = await mediator.GetRequestWorkflowAsync(dbRequest.Id);
                    var workflow = WorkflowDefinition.ResolveWorkflow(dbWorkflow);

                    if (dbRequest.State.State is null)
                        throw new InvalidOperationException("Workflow has not been initialized");


                    var currentStep = workflow[dbRequest.State.State];
                    await mediator.Publish(
                        new CanApproveStep(dbRequest.Id, dbRequest.Type, currentStep.Id, currentStep.NextStepId),
                        cancellationToken);

                    if (!workflow.HasNextStep())
                        throw new InvalidWorkflowError("The request has no next step to approve", []);

                    currentStep = workflow.CompleteCurrentStep(DbWFStepState.Approved, request.Editor.Person);
                    dbRequest.State.State = workflow.GetCurrent().Id;

                    workflow.SaveChanges();

                    await dbContext.SaveChangesAsync(cancellationToken);

                    INotification notification = new RequestStateChanged(dbRequest.Id, dbRequest.Type,
                        currentStep?.PreviousStepId, currentStep?.Id);
                    await mediator.Publish(notification, cancellationToken);


                    if (!string.Equals(dbRequest.State.State, WorkflowDefinition.APPROVAL,
                            StringComparison.OrdinalIgnoreCase))
                        return;


                    // TODO: Hear with PO whether to lock the proposed candidate, if not
                    // Then we need to save the original proposed candidate and check if proposed candidate is changed.
                    var anyProposedChanges = !string.IsNullOrWhiteSpace(dbRequest.ProposedChanges) &&
                                             JObject.Parse(dbRequest.ProposedChanges).HasValues;

                    // For a direct allocation, we can auto complete the request if no changes has been proposed.
                    if (workflow is AllocationDirectWorkflowV1 directWorkflow && !anyProposedChanges)
                    {
                        currentStep = directWorkflow.AutoApproveUnchangedRequest();
                        dbRequest.State.State = workflow.GetCurrent().Id;

                        workflow.SaveChanges();
                        await dbContext.SaveChangesAsync(CancellationToken.None);


                        notification = new RequestStateChanged(dbRequest.Id, dbRequest.Type,
                            currentStep?.PreviousStepId, currentStep?.Id);
                        await mediator.Publish(notification, CancellationToken.None);

                        notification = new InternalRequestNotifications.ProposedPersonAutoApproved(dbRequest.Id);
                        await mediator.Publish(notification, CancellationToken.None);
                    }
                    else
                    {
                        await mediator.Publish(new InternalRequestNotifications.ProposedPerson(request.RequestId),
                            CancellationToken.None);
                    }
                }
            }
        }
    }
}