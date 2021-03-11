using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {

        public partial class ProjectChange
        {
            public class InitializedHandler : INotificationHandler<RequestInitialized>
            {
                private readonly ResourcesDbContext dbContext;
                private readonly IProjectOrgResolver orgResolver;

                public InitializedHandler(ResourcesDbContext dbContext, IProjectOrgResolver orgResolver)
                {
                    this.dbContext = dbContext;
                    this.orgResolver = orgResolver;
                }

                public async Task Handle(RequestInitialized notification, CancellationToken cancellationToken)
                {
                    if (notification.Type != DbInternalRequestType.TaskOwnerChange)
                        return;

                    var initiatedBy = await dbContext.Persons.FirstAsync(p => p.Id == notification.InitiatedByDbPersonId);
                    var request = await dbContext.ResourceAllocationRequests.FirstAsync(r => r.Id == notification.RequestId);

                    var workflow = await ResolveWorkflowTypeAsync(request, initiatedBy);

                    ValidateWorkflow(workflow, request);

                    StartWorkflow(workflow, request);

                    await dbContext.SaveChangesAsync();
                }

                private void ValidateWorkflow(WorkflowDefinition workflow, DbResourceAllocationRequest request)
                {
                    var isFutureSplit = request.OrgPositionInstance.AppliesFrom > DateTime.UtcNow.Date;
                    var isExpiredSplit = request.OrgPositionInstance.AppliesTo < DateTime.UtcNow.Date;

                    var hasChanges = !string.IsNullOrEmpty(request.ProposedChanges);
                    var hasPersonChange = request.ProposedPerson.HasBeenProposed;
                    
                    var hasChangeDate = request.ApplicableChangeDate != null;

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

                private async Task<WorkflowDefinition> ResolveWorkflowTypeAsync(DbResourceAllocationRequest request, DbPerson initiatedBy)
                {
                    if (request.OrgPositionId is null)
                        throw new InvalidOperationException("Org position must be set");

                    var position = await orgResolver.ResolvePositionAsync(request.OrgPositionId.Value);
                    if (position is null) throw new InvalidOperationException("Could not resolve org position");

                    var basePosition = await orgResolver.ResolveBasePositionAsync(position.BasePosition.Id);
                    var bpSettings = basePosition!.GetTypedSettings();

                    // Start direct change request
                    if (bpSettings.DirectAssignmentEnabled.GetValueOrDefault(false))
                    {
                        return new TaskOwnerChangeDirectWorkflowV1(initiatedBy);
                    }
                    else
                    {
                        // Check if joint venture
                        var instance = position.Instances.First(i => i.Id == request.OrgPositionInstance.Id);

                        if (string.Equals(instance.Properties.GetProperty<string>("type", "normal"), "jointVenture", StringComparison.OrdinalIgnoreCase))
                        {
                            return new TaskOwnerChangeJointVentrueWorkflowV1(initiatedBy);
                        }
                        else
                        {
                            return new TaskOwnerChangeNormalWorkflowV1(initiatedBy);
                        }
                    }
                }

                private void StartWorkflow(WorkflowDefinition workflow, DbResourceAllocationRequest request)
                {
                    switch (workflow)
                    {
                        case TaskOwnerChangeDirectWorkflowV1:
                            request.SubType = "direct";
                            break;

                        case TaskOwnerChangeJointVentrueWorkflowV1:
                            request.SubType = "jointVenture";
                            break;

                        case TaskOwnerChangeNormalWorkflowV1:
                            request.SubType = "normal";
                            break;
                    }

                    dbContext.Workflows.Add(workflow.CreateDatabaseEntity(request.Id, DbRequestType.InternalRequest));

                    request.LastActivity = DateTime.UtcNow;
                    request.State.State = workflow.GetCurrent().PreviousStepId;
                }
            }
        }
    }
}