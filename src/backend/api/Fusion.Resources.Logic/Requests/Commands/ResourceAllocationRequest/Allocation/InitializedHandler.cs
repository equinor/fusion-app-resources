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
        public partial class Allocation
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
                    if (notification.Type != DbInternalRequestType.Allocation)
                        return;

                    var initiatedBy = await dbContext.Persons.FirstAsync(p => p.Id == notification.InitiatedByDbPersonId);
                    var request = await dbContext.ResourceAllocationRequests.FirstAsync(r => r.Id == notification.RequestId);

                    var subType = notification.SubType;

                    if (subType is null)
                    {
                        if (request.OrgPositionId is null)
                            throw new InvalidOperationException("Org position id is null for request. Cannot resolve sub type.");

                        subType = await mediator.Send(new ResolveSubType(request.OrgPositionId.Value, request.OrgPositionInstance.Id));
                    }

                    WorkflowDefinition workflow = subType.ToLower() switch
                    {
                        AllocationDirectWorkflowV1.SUBTYPE => new AllocationDirectWorkflowV1(initiatedBy),
                        AllocationJointVentureWorkflowV1.SUBTYPE => new AllocationJointVentureWorkflowV1(initiatedBy),
                        AllocationNormalWorkflowV1.SUBTYPE => new AllocationNormalWorkflowV1(initiatedBy),
                        _ => throw new NotSupportedException($"Sub type '{subType}' is not supported for initialization.")
                    };

                    dbContext.Workflows.Add(workflow.CreateDatabaseEntity(notification.RequestId, DbRequestType.InternalRequest));

                    request.LastActivity = DateTime.UtcNow;
                    request.State.State = AllocationDirectWorkflowV1.CREATED;

                    // Try to resolve the default assigned department
                    if (request.AssignedDepartment is null)
                        request.AssignedDepartment = await mediator.Send(new Queries.ResolveResponsibleDepartment(request.Id));


                    await dbContext.SaveChangesAsync();

                    await mediator.Publish(new AllocationRequestStarted(request.Id, workflow));
                }


            }
        }
    }
}
