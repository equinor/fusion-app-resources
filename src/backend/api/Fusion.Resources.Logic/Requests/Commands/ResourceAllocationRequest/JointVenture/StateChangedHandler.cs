using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Fusion.Resources.Logic.Workflows;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class JointVenture
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
                    if (notification.Type != Database.Entities.DbInternalRequestType.JointVenture)
                        return;

                    if (notification.ToState == InternalRequestJointVentureWorkflowV1.PROVISIONING)
                    {
                        await mediator.Send(new QueueProvisioning(notification.RequestId));
                    }
                }
            }
        }
    }
}
