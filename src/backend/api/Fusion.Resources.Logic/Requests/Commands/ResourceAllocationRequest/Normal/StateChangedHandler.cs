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
                private readonly IMediator mediator;

                public StateChangedHandler(IMediator mediator)
                {
                    this.mediator = mediator;
                }

                public async Task Handle(RequestStateChanged notification, CancellationToken cancellationToken)
                {
                    if (notification.Type != Database.Entities.DbInternalRequestType.Normal)
                        return;

                    if (notification.ToState == InternalRequestNormalWorkflowV1.PROVISIONING)
                    {
                        await mediator.Send(new QueueProvisioning(notification.RequestId));
                    }
                }
            }
        }
    }
}