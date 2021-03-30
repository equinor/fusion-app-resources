using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Fusion.Resources.Logic.Workflows;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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


                    var subType = new SubType(request.SubType);

                    switch (subType.Value)
                    {
                        case SubType.Types.Adjustment:
                            if ((!hasChanges && !hasPersonChange) || (!isFutureSplit && !hasChangeDate))
                                throw InvalidWorkflowError.ValidationError("Required properties are missing in order to start the workflow.", s =>
                                {
                                    var changes = JsonConvert.DeserializeAnonymousType(request.ProposedChanges ?? "{}", new { workload = (double?)null, location = new { } });

                                    var hasAdjustmentChanges = changes.workload is null && changes.location is null;

                                    if (!isFutureSplit && !hasChangeDate)
                                        s.AddFailure("proposalParameters.changeDateFrom", "When the instance to change is currently active, a date the change is going to take effect is required.");

                                    if  (hasAdjustmentChanges)
                                        s.AddFailure("changes", "Either proposed changes or proposed person must be set.");

                                }).SetWorkflowName(workflow);
                            break;

                        case SubType.Types.ChangeResource:
                            if ((!hasChanges && !hasPersonChange) || (!isFutureSplit && !hasChangeDate))
                                throw InvalidWorkflowError.ValidationError("Required properties are missing in order to start the workflow.", s =>
                                {
                                    if (!isFutureSplit && !hasChangeDate)
                                        s.AddFailure("proposalParameters.changeDateFrom", "When the instance to change is currently active, a date the change is going to take effect is required.");

                                    // Change request requires that there is defined a person in the proposed changes
                                    var changes = JsonConvert.DeserializeAnonymousType(request.ProposedChanges ?? "{}", new { assignedPerson = new { AzureUniqueId = Guid.Empty } });

                                    var hasPersonToChangeTo = hasPersonChange;
                                    //var hasPersonToChangeTo = (changes.assignedPerson?.AzureUniqueId).GetValueOrDefault(Guid.Empty) != Guid.Empty;
                                    //var hasPersonToChangeFrom = hasPersonChange;

                                    //if (!hasPersonToChangeTo)
                                    //    s.AddFailure("proposedChanges.assignedPerson.azureUniqueId", "Must specify person to change to");

                                    if (hasPersonToChangeTo)
                                        s.AddFailure("proposedPersonAzureUniqueId", "Must specify person to change to");

                                }).SetWorkflowName(workflow);
                            break;

                        case SubType.Types.RemoveResource:
                            if (!isFutureSplit && !hasChangeDate)
                                throw InvalidWorkflowError.ValidationError("Required properties are missing in order to start the workflow.", s =>
                                {
                                    if (!isFutureSplit && !hasChangeDate)
                                        s.AddFailure("proposalParameters.changeDateFrom", "When the instance to change is currently active, a date the change is going to take effect is required.");

                                    if (request.OrgPositionInstance.AssignedToUniqueId is null)
                                        s.AddFailure("instance.assignedPerson", "Must specify person to unassign");

                                    //if (hasPersonChange)
                                    //    s.AddFailure("proposedPersonAzureUniqueId", "Must specify person to unassign");

                                }).SetWorkflowName(workflow);
                            break;

                        default:
                            throw InvalidWorkflowError.ValidationError($"Sub type '{request.SubType}' is not valid")
                                .SetWorkflowName(workflow);
                    }                    
                }
            }
        }
    }
}
