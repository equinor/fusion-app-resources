using Fusion.Resources.Domain;
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

                public Task Handle(RequestStateChanged notification, CancellationToken cancellationToken)
                {
                    if (notification.Type != InternalRequestType.Normal)
                        return Task.CompletedTask;

                    if (notification.ToState == InternalRequestNormalWorkflowV1.PROVISIONING)
                    {
                        // Queue provisiong
                    }

                    return Task.CompletedTask;
                }
            }
        }
    }
}