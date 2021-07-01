using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Notification;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using MediatR;

namespace Fusion.Resources.Api.Notifications
{
    public partial class InternalRequestNotification
    {
        public class NotifyTaskOwnerHandler : AsyncRequestHandler<NotifyTaskOwner>
        {
            private readonly IFusionNotificationClient notificationClient;
            private readonly IMediator mediator;

            public NotifyTaskOwnerHandler(IFusionNotificationClient notificationClient, IMediator mediator)
            {
                this.notificationClient = notificationClient;
                this.mediator = mediator;
            }
            protected override async Task Handle(NotifyTaskOwner request, CancellationToken cancellationToken)
            {
                var allocationRequest = await GetInternalRequestAsync(request.RequestId);

                var arguments = new NotificationArguments($"A personnel request has been updated") { AppKey = "personnel-allocation" };
                if (allocationRequest?.TaskOwner?.Persons is null)
                    return;

                foreach (var recipient in allocationRequest.TaskOwner.Persons.Where(x => x.AzureUniqueId != request.Editor.Person.AzureUniqueId && x.AzureUniqueId.HasValue))
                {
                    await notificationClient.CreateNotificationForUserAsync(recipient.AzureUniqueId!, arguments, request.Card);
                }
            }

            private async Task<QueryResourceAllocationRequest?> GetInternalRequestAsync(Guid requestId)
            {
                var query = new GetResourceAllocationRequestItem(requestId).ExpandTaskOwner();
                var request = await mediator.Send(query);
                return request;
            }
        }
    }
}