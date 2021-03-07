using Fusion.Resources.Database.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Fusion.Resources.Domain;
using Fusion.Resources.Logic.Workflows;
using Microsoft.EntityFrameworkCore;
using Fusion.Resources.Integration.Models.Queue;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class Direct
        {
            public class InitializedHandler : INotificationHandler<RequestInitialized>
            {
                private readonly ResourcesDbContext dbContext;
                private readonly IQueueSender queueSender;

                public InitializedHandler(ResourcesDbContext dbContext, IQueueSender queueSender)
                {
                    this.dbContext = dbContext;
                    this.queueSender = queueSender;
                }

                public async Task Handle(RequestInitialized notification, CancellationToken cancellationToken)
                {
                    if (notification.Type != InternalRequestType.Direct)
                        return;

                    var initiatedBy = await dbContext.Persons.FirstAsync(p => p.Id == notification.InitiatedByDbPersonId);
                    var request = await dbContext.ResourceAllocationRequests.FirstAsync(r => r.Id == notification.RequestId);


                    var workflow = new InternalRequestNormalWorkflowV1(initiatedBy);
                    dbContext.Workflows.Add(workflow.CreateDatabaseEntity(notification.RequestId, DbRequestType.InternalRequest));

                    request.LastActivity = DateTime.UtcNow;
                    request.State.State = InternalRequestNormalWorkflowV1.CREATED;

                    await dbContext.SaveChangesAsync();


                    // Workflow has no more steps, queue provisioning
                    await queueSender.SendMessageAsync(QueuePath.ProvisionPosition, new ProvisionPositionMessageV1
                    {
                        RequestId = request.Id,
                        ProjectOrgId = request.Project.OrgProjectId,
                        Type = ProvisionPositionMessageV1.RequestTypeV1.InternalPersonnel
                    });
                }
            }
        }
    }
}
