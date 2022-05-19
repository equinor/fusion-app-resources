using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain.Notifications.InternalRequests;
using Fusion.Resources.Domain.Queries;
using MediatR;

namespace Fusion.Resources.Api.FusionEvents.Handlers.InternalRequests
{
    public class UpdateOrgPositionInstanceHaveRequestHandler : INotificationHandler<InternalRequestCreated>, INotificationHandler<InternalRequestDeleted>
    {
        private readonly IMediator mediator;

        public UpdateOrgPositionInstanceHaveRequestHandler(IMediator mediator)
        {
            this.mediator = mediator;
        }
        public async Task Handle(InternalRequestCreated notification, CancellationToken cancellationToken)
        {
            var request = await mediator.Send(new GetResourceAllocationRequestItem(notification.RequestId));
            if (request is null)
                return;

            await mediator.Send(new Logic.Commands.UpdateOrgPositionInstanceHaveRequest(request.Project.OrgProjectId, request.OrgPosition!.Id, request.OrgPositionInstance!.Id, true));
        }

        public async Task Handle(InternalRequestDeleted notification, CancellationToken cancellationToken)
        {
            await mediator.Send(new Logic.Commands.UpdateOrgPositionInstanceHaveRequest(notification.OrgProjectId, notification.OrgPositionId!.Value, notification.PositionInstanceId!.Value, false));
        }
    }
}