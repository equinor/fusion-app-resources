﻿using Fusion.Resources.Database;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Notifications.InternalRequests;
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
                private readonly IProfileService profileService;

                public Handler(ResourcesDbContext dbContext, IMediator mediator, IProfileService profileService)
                {
                    this.dbContext = dbContext;
                    this.mediator = mediator;
                    this.profileService = profileService;
                }

                public async Task Handle(Approve request, CancellationToken cancellationToken)
                {
                    var dbRequest = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);
                    if (dbRequest is null)
                        throw new InvalidOperationException("Could not locate request");

                    var dbWorkflow = await mediator.GetRequestWorkflowAsync(dbRequest.Id);
                    var workflow = WorkflowDefinition.ResolveWorkflow(dbWorkflow);

                    if (dbRequest.State.State is null)
                        throw new InvalidOperationException("Workflow has not been initialized");


                    var currentStep = workflow[dbRequest.State.State];
                    await mediator.Publish(new CanApproveStep(dbRequest.Id, dbRequest.Type, currentStep.Id, currentStep.NextStepId), cancellationToken);

                    if (!workflow.HasNextStep())
                        throw new InvalidWorkflowError("The request has no next step to approve", []);

                    currentStep = workflow.CompleteCurrentStep(DbWFStepState.Approved, request.Editor.Person);
                    dbRequest.State.State = workflow.GetCurrent().Id;

                    workflow.SaveChanges();

                    await dbContext.SaveChangesAsync(cancellationToken);

                    INotification notification = new RequestStateChanged(dbRequest.Id, dbRequest.Type, currentStep?.PreviousStepId, currentStep?.Id);
                    await mediator.Publish(notification, cancellationToken);


                    if (!string.Equals(dbRequest.State.State, WorkflowDefinition.APPROVAL, StringComparison.OrdinalIgnoreCase))
                        return;

                    // For a direct allocation, we can auto complete the request if no changes has been proposed.
                    if (workflow is AllocationDirectWorkflowV1 directWorkflow && AnyProposedChanges(dbRequest) == false)
                    {
                        var systemAccount = await profileService.EnsureSystemAccountAsync();
                        currentStep = directWorkflow.AutoAcceptedUnchangedRequest(systemAccount);
                        dbRequest.State.State = workflow.GetCurrent().Id;

                        workflow.SaveChanges();
                        await dbContext.SaveChangesAsync(CancellationToken.None);


                        notification = new RequestStateChanged(dbRequest.Id, dbRequest.Type, currentStep?.PreviousStepId, currentStep?.Id);
                        await mediator.Publish(notification, CancellationToken.None);

                        notification = new InternalRequestNotifications.ProposedPersonAutoAccepted(dbRequest.Id);
                        await mediator.Publish(notification, CancellationToken.None);
                    }
                    else
                    {
                        await mediator.Publish(new InternalRequestNotifications.ProposedPerson(request.RequestId), CancellationToken.None);
                    }
                }

                /// <summary>
                ///     AnyProposedChanges for a direct allocation
                /// </summary>
                private static bool AnyProposedChanges(DbResourceAllocationRequest dbRequest)
                {
                    if (!dbRequest.ProposedPerson.HasBeenProposed)
                        throw new InvalidOperationException("Proposed person has not been set for a direct allocation");

                    // For older requests, we don't have the InitialProposedPerson property
                    // Assume for these that the proposed person has been changed
                    var hasProposedPersonBeenChanged = dbRequest.InitialProposedPerson is null
                                                       ||
                                                        dbRequest.InitialProposedPerson.AzureUniqueId !=
                                                        dbRequest.ProposedPerson.AzureUniqueId
                                                        ||
                                                        dbRequest.InitialProposedPerson.Mail !=
                                                       dbRequest.ProposedPerson.Mail;
                    bool hasProposedChanges;
                    try
                    {
                        hasProposedChanges = !string.IsNullOrWhiteSpace(dbRequest.ProposedChanges) &&
                                             JObject.Parse(dbRequest.ProposedChanges).HasValues;
                    }
                    catch (Exception e)
                    {
                        // If we can't parse the proposed changes, we should not continue
                        throw new InvalidOperationException("Could not parse proposed changes", e);
                    }

                    return hasProposedPersonBeenChanged || hasProposedChanges;
                }
            }
        }
    }
}