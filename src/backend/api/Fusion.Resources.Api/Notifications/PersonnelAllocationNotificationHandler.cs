using Fusion.ApiClients.Org;
using Fusion.Integration.Notification;
using Fusion.Resources.Api.Notifications.Markdown;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Notifications;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Notifications
{
    public class PersonnelAllocationNotificationHandler : INotificationHandler<PersonnelAddedToContract>
    {
        private readonly IMediator mediator;
        private readonly IFusionNotificationClient notificationClient;
        private readonly IOrgApiClient orgClient;

        public PersonnelAllocationNotificationHandler(IMediator mediator, IFusionNotificationClient notificationClient, IOrgApiClientFactory orgApiClientFactory)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
            orgClient = orgApiClientFactory.CreateClient(ApiClientMode.Application);
        }

        public async Task Handle(PersonnelAddedToContract notification, CancellationToken cancellationToken)
        {
            var personnelItem = await mediator.Send(new GetContractPersonnelItem(notification.OrgContractId, notification.ContractPersonnelId));

            if (personnelItem?.AzureUniqueId == null)
                return;

            var project = await orgClient.GetProjectOrDefaultV2Async(notification.OrgProjectId);
            var contract = await orgClient.GetContractV2Async(notification.OrgProjectId, notification.OrgContractId);

            await notificationClient.CreateNotificationAsync(n => n
                .WithRecipient(personnelItem.AzureUniqueId)
                .WithTitle($"You were added to contract {contract.Name}")
                .WithDescriptionMarkdown(NotificationDescription.PersonnelAllocated(personnelItem, contract, project)));
        }

        private static class NotificationDescription
        {
            public static string PersonnelAllocated(QueryContractPersonnel personnelItem, ApiProjectContractV2 contract, ApiProjectV2 project)
            {
                var description = new MarkdownDocument()
                .Paragraph($"{MdToken.Bold(personnelItem.CreatedBy.Name)} ({personnelItem.CreatedBy.Mail}) added you to contract {contract.Name}")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {contract?.ContractNumber}"));

                if (personnelItem.AzureAdStatus == Database.Entities.DbAzureAccountStatus.NoAccount)
                {
                    description = description.Paragraph($"You do not yet have a guest/affiliate account, which is required. " +
                        $"Please contact {personnelItem.CreatedBy.Name} ({personnelItem.CreatedBy.Mail}) to have one created for you");
                }
                else if (personnelItem.AzureAdStatus == Database.Entities.DbAzureAccountStatus.InviteSent)
                {
                    description = description.Paragraph($"You have not accepted the invitation email to join Equinor directory as a guest/affiliate user. " +
                        $"Please do so, or contact {personnelItem.CreatedBy.Name} ({personnelItem.CreatedBy.Mail}) if you have questions");
                }

                return description.Build();
            }
        }
    }
}
