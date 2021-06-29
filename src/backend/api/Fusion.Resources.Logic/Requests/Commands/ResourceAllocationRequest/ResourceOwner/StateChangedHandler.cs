using System;
using Fusion.Resources.Database.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Notifications.InternalRequests;
using Fusion.Resources.Logic.Workflows;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class ResourceOwner
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
                    if (notification.Type != DbInternalRequestType.ResourceOwnerChange)
                        return;

                    if (string.Equals(notification.ToState, AllocationNormalWorkflowV1.APPROVAL, StringComparison.OrdinalIgnoreCase))
                        await mediator.Publish(new InternalRequestNotifications.ProposedPerson(notification.RequestId));

                    if (notification.ToState == WorkflowDefinition.PROVISIONING)
                    {

                        await mediator.Send(new QueueProvisioning(notification.RequestId));
                    }
                }
            }
        }
    }
}
