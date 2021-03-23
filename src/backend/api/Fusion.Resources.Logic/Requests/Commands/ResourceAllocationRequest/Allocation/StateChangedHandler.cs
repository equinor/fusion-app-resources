using Fusion.Resources.Database.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Logic.Workflows;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class Allocation
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
                    if (notification.Type != DbInternalRequestType.Allocation)
                        return;

                    if (notification.ToState == WorkflowDefinition.PROVISIONING)
                    {
                        await mediator.Send(new QueueProvisioning(notification.RequestId));
                    }
                }
            }
        }
    }
}
