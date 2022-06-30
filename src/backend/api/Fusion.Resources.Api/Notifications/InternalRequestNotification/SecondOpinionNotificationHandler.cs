using Fusion.Integration.Notification;
using Fusion.Resources.Domain;
using MediatR;
using System.Threading;
using System.Threading.Tasks;


namespace Fusion.Resources.Api.Notifications
{
    public class SecondOpinionNotificationHandler
        : INotificationHandler<SecondOpinionRequested>
    {
        private readonly IFusionNotificationClient notificationClient;
        private readonly INotificationBuilder notificationBuilder;

        public SecondOpinionNotificationHandler(IFusionNotificationClient notificationClient, INotificationBuilderFactory notificationBuilderFactory)
        {
            this.notificationClient = notificationClient;
            this.notificationBuilder = notificationBuilderFactory.CreateDesigner();
        }

        public async Task Handle(SecondOpinionRequested notification, CancellationToken cancellationToken)
        {
            var arguments = new NotificationArguments("Your opinion has been requested on a personnel request.") { AppKey = "personnel-allocation" };
            var card = await notificationBuilder
                .AddTitle("Your opinion has been requested on a personnel request.")
                .TryAddProfileCard(notification.Request.OrgPositionInstance?.AssignedPerson?.AzureUniqueId)
                .AddFacts(x => x
                    .AddFactIf("Proposed Person", $"{notification.Request?.ProposedPerson?.Person?.Name}", notification.Request?.ProposedPerson?.Person is not null)
                    .AddFactIf("Request number", $"{notification.Request?.RequestNumber}", notification.Request?.RequestNumber is not null)
                    .AddFactIf("Project", $"{notification.Request?.Project?.Name}", notification.Request?.Project is not null)
                    .AddFactIf("Position id", $"{notification.Request?.OrgPosition?.ExternalId}", notification.Request?.OrgPosition?.ExternalId is not null)
                    .AddFactIf("Position", $"{notification.Request?.OrgPosition?.Name}", notification.Request?.OrgPosition?.Name is not null)
                )
                .AddTextBlock($"Created by: {notification.Request.CreatedBy.Name}")
                .TryAddOpenPortalUrlAction("Give your opinion in the Personnel Allocation App", $"aka/goto-second-opinion/{notification.SecondOpinion.Id}")
                .BuildCardAsync();

            await notificationClient.CreateNotificationForUserAsync(notification.Person.AzureUniqueId, arguments, card);
        }
    }
}
