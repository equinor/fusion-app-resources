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
        public class NotifyRequestCreatorHandler : AsyncRequestHandler<NotifyRequestCreator>
        {
            private readonly IFusionNotificationClient notificationClient;
            private readonly IMediator mediator;

            public NotifyRequestCreatorHandler(IFusionNotificationClient notificationClient, IMediator mediator)
            {
                this.notificationClient = notificationClient;
                this.mediator = mediator;
            }
            protected override async Task Handle(NotifyRequestCreator request, CancellationToken cancellationToken)
            {
                var allocationRequest = await GetInternalRequestAsync(request.RequestId);

                var arguments = new NotificationArguments($"A personnel request you created has been updated") { AppKey = "personnel-allocation" };

                if(allocationRequest?.CreatedBy.AzureUniqueId != request.Editor.Person.AzureUniqueId)
                {
                    await notificationClient.CreateNotificationForUserAsync(allocationRequest?.CreatedBy.AzureUniqueId!, arguments, request.Card);
                }
            }

            private async Task<QueryResourceAllocationRequest?> GetInternalRequestAsync(Guid requestId)
            {
                var query = new GetResourceAllocationRequestItem(requestId);
                var request = await mediator.Send(query);
                return request;
            }
        }
    }
}