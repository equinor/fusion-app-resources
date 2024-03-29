﻿using Fusion.Resources.Database;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Notifications.InternalRequests;

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
                    var dbRequest = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);
                    if (dbRequest is null)
                        throw new InvalidOperationException("Could not locate request");

                    var dbWorkflow = await mediator.GetRequestWorkflowAsync(dbRequest.Id);
                    var workflow = WorkflowDefinition.ResolveWorkflow(dbWorkflow);

                    if (dbRequest.State.State is null)
                        throw new InvalidOperationException("Workflow has not been initialized");


                    var currentStep = workflow[dbRequest.State.State];
                    await mediator.Publish(new CanApproveStep(dbRequest.Id, dbRequest.Type, currentStep.Id, currentStep.NextStepId), cancellationToken);

                    currentStep = workflow.CompleteCurrentStep(Database.Entities.DbWFStepState.Approved, request.Editor.Person);
                    dbRequest.State.State = workflow.GetCurrent().Id;

                    workflow.SaveChanges();

                    await dbContext.SaveChangesAsync(cancellationToken);

                    var notification = new RequestStateChanged(dbRequest.Id, dbRequest.Type, currentStep?.PreviousStepId, currentStep?.Id);
                    await mediator.Publish(notification, cancellationToken);

                    if (string.Equals(dbRequest.State.State, WorkflowDefinition.APPROVAL, StringComparison.OrdinalIgnoreCase))
                        await mediator.Publish(new InternalRequestNotifications.ProposedPerson(request.RequestId));

                }
            }
        }
    }
}