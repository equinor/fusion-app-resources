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
            public class DirectAllocationRequestStarted : INotificationHandler<AllocationRequestStarted>
            {
                private readonly IMediator mediator;

                public DirectAllocationRequestStarted(IMediator mediator)
                {
                    this.mediator = mediator;
                }

                public async Task Handle(AllocationRequestStarted notification, CancellationToken cancellationToken)
                {
                    if (notification.Workflow is not AllocationDirectWorkflowV1)
                        return;

                    await mediator.Send(new QueueProvisioning(notification.RequestId));
                }
            }
        }
    }
}
