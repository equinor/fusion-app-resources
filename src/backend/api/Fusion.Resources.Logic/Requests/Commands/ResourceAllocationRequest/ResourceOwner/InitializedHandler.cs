using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Fusion.Resources.Logic.Workflows;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class ResourceOwner
        {

            public class InitializedHandler : INotificationHandler<RequestInitialized>
            {
                private readonly ResourcesDbContext dbContext;
                private readonly IMediator mediator;

                public InitializedHandler(ResourcesDbContext dbContext, IMediator mediator)
                {
                    this.dbContext = dbContext;
                    this.mediator = mediator;
                }

                public async Task Handle(RequestInitialized notification, CancellationToken cancellationToken)
                {
                    if (notification.Type != DbInternalRequestType.ResourceOwnerChange)
                        return;

                    var initiatedBy = await dbContext.Persons.FirstAsync(p => p.Id == notification.InitiatedByDbPersonId);
                    var request = await dbContext.ResourceAllocationRequests.FirstAsync(r => r.Id == notification.RequestId);


                    var workflow = new ResourceOwnerChangeWorkflowV1(initiatedBy);

                    ValidateWorkflow(workflow, request);


                    dbContext.Workflows.Add(workflow.CreateDatabaseEntity(notification.RequestId, DbRequestType.InternalRequest));

                    request.LastActivity = DateTime.UtcNow;
                    request.State.State = AllocationDirectWorkflowV1.CREATED;

                    // Try to resolve the default assigned department
                    await dbContext.SaveChangesAsync();

                    //await mediator.Publish(new AllocationRequestStarted(request.Id, workflow));
                }


                private void ValidateWorkflow(ResourceOwnerChangeWorkflowV1 workflow, DbResourceAllocationRequest request)
                {
                    var isFutureSplit = request.OrgPositionInstance.AppliesFrom > DateTime.UtcNow.Date;
                    var isExpiredSplit = request.OrgPositionInstance.AppliesTo < DateTime.UtcNow.Date;

                    var hasChanges = !string.IsNullOrEmpty(request.ProposedChanges);
                    var hasPersonChange = request.ProposedPerson.HasBeenProposed;

                    var hasChangeDate = request.ProposalParameters.ChangeFrom != null || request.ProposalParameters.ChangeTo != null;


                    if (isExpiredSplit)
                        throw InvalidWorkflowError.ValidationError("Cannot create change request on expired instance.")
                            .SetWorkflowName(workflow);

                    // Check that the workflow can be started. This requires that a person is proposed.
                    if ((!hasChanges && !hasPersonChange) || (!isFutureSplit && !hasChangeDate))
                        throw InvalidWorkflowError.ValidationError("Required properties are missing in order to start the workflow.", s =>
                        {
                            if (!isFutureSplit && !hasChangeDate)
                                s.AddFailure("applicableChangeDate", "When the instance to change is currently active, a date the change is going to take effect is required.");

                            if (!hasChanges && !hasPersonChange)
                                s.AddFailure("changes", "Either proposed changes or proposed person must be set.");
                        }).SetWorkflowName(workflow);
                }
            }
        }
    }
}
