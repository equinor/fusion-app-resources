using Fusion.Integration.Notification;
using Fusion.Resources.Api.Notifications.Markdown;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Notifications;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Notifications
{
    public class PersonnelAllocationNotificationHandler : INotificationHandler<PersonnelAddedToContract>
    {
        private readonly IMediator mediator;
        private readonly IFusionNotificationClient notificationClient;

        public PersonnelAllocationNotificationHandler(IMediator mediator, IFusionNotificationClient notificationClient)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
        }
        public async Task Handle(PersonnelAddedToContract notification, CancellationToken cancellationToken)
        {
            var personnelItem = await mediator.Send(new GetContractPersonnelItem(notification.OrgContractId, notification.ContractPersonnelId));
            await notificationClient.CreateNotificationAsync(n => n
                .WithRecipient("")
                .WithDescriptionMarkdown(NotificationDescription.PersonnelAllocated(personnelItem)));
        }

        private static class NotificationDescription
        {
            public static string PersonnelAllocated(QueryContractPersonnel personnelItem) => new MarkdownDocument()
                .Paragraph($"{MdToken.Bold(personnelItem.CreatedBy.Name)}")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")}"))
                .Build();
        }
    }
}
