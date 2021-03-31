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

                public InitializedHandler(ResourcesDbContext dbContext)
                {
                    this.dbContext = dbContext;
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

                    await dbContext.SaveChangesAsync();
                }

                private void ValidateWorkflow(ResourceOwnerChangeWorkflowV1 workflow, DbResourceAllocationRequest request)
                {
                    var validator = new RequestValidator();
                    var result = validator.Validate(request);

                    if (!result.IsValid)
                    {
                        throw new InvalidWorkflowError("Required properties are missing in order to start the workflow.", result.Errors)
                            .SetWorkflowName(workflow);
                    }
                }
            }
        }
    }
}
