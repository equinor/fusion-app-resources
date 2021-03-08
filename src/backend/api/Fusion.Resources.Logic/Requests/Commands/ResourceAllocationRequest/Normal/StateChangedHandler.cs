using Fusion.Resources.Database;
using Fusion.Resources.Domain;
using Fusion.Resources.Integration.Models.Queue;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {

        public partial class Normal
        {
            public class StateChangedHandler : INotificationHandler<RequestStateChanged>
            {
                private readonly ResourcesDbContext dbContext;
                private readonly IQueueSender queueSender;

                public StateChangedHandler(ResourcesDbContext dbContext, IQueueSender queueSender)
                {
                    this.dbContext = dbContext;
                    this.queueSender = queueSender;
                }

                public async Task Handle(RequestStateChanged notification, CancellationToken cancellationToken)
                {
                    if (notification.Type != Database.Entities.DbInternalRequestType.Normal)
                        return;

                    var request = await dbContext.ResourceAllocationRequests.FindAsync(notification.RequestId);

                    if (notification.ToState == InternalRequestNormalWorkflowV1.PROVISIONING)
                    {
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
}