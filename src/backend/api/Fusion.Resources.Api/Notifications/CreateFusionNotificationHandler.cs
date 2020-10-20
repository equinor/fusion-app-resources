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
    public class CreateFusionNotificationHandler :
        INotificationHandler<ContractRoleDelegated>,
        INotificationHandler<DelegatedContractRoleRecertified>,
        INotificationHandler<CompanyRepUpdated>,
        INotificationHandler<ContractRepUpdated>,
        INotificationHandler<ExternalCompanyRepUpdated>,
        INotificationHandler<ExternalContractRepUpdated>
    {
        private readonly IMediator mediator;
        private readonly IFusionNotificationClient notificationClient;
        private readonly IOrgApiClient orgApiClient;

        public CreateFusionNotificationHandler(IMediator mediator, IFusionNotificationClient notificationClient, IOrgApiClientFactory apiClientFactory)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
            this.orgApiClient = apiClientFactory.CreateClient(ApiClientMode.Application);
        }

        public async Task Handle(ContractRoleDelegated notification, CancellationToken cancellationToken)
        {
            var delegatedRole = await mediator.Send(new Domain.GetContractDelegatedRole(notification.RoleId));

            if (delegatedRole == null)
                return;


            await notificationClient.CreateNotificationAsync(notification => notification
                    .WithRecipient(delegatedRole.Person.AzureUniqueId)
                    .WithTitle($"You were delegated {delegatedRole.Type} role in {delegatedRole.Contract.ContractNumber} - {delegatedRole.Project.Name}")
                    .WithDescriptionMarkdown(NotificationDescription.DelegateAssigned(delegatedRole)));
        }

        public async Task Handle(DelegatedContractRoleRecertified notification, CancellationToken cancellationToken)
        {
            var delegatedRole = await mediator.Send(new Domain.GetContractDelegatedRole(notification.RoleId));

            if (delegatedRole == null)
                return;


            await notificationClient.CreateNotificationAsync(notification => notification
                    .WithRecipient(delegatedRole.Person.AzureUniqueId)
                    .WithTitle($"Your delegated role in {delegatedRole.Contract.ContractNumber} - {delegatedRole.Project.Name} was recertified")
                    .WithDescriptionMarkdown(NotificationDescription.DelegateRecertified(delegatedRole)));
        }

        public async Task Handle(CompanyRepUpdated notification, CancellationToken cancellationToken)
        {
            var position = await orgApiClient.GetPositionV2Async(notification.PositionId);
            var instance = position?.GetActiveInstance();

            if (position == null || instance == null || instance.AssignedPerson == null || instance.AssignedPerson.AzureUniqueId == null)
                return;

            await notificationClient.CreateNotificationAsync(notification => notification
                .WithRecipient(instance.AssignedPerson.AzureUniqueId)
                .WithTitle($"You were allocated as Company Rep in contract '{position.Contract.Name} ({position.Contract.ContractNumber})'")
                .WithDescriptionMarkdown(NotificationDescription.PositionAssigned(position, instance)));
        }

        public async Task Handle(ContractRepUpdated notification, CancellationToken cancellationToken)
        {
            var position = await orgApiClient.GetPositionV2Async(notification.PositionId);
            var instance = position?.GetActiveInstance();

            if (position == null || instance == null || instance.AssignedPerson == null || instance.AssignedPerson.AzureUniqueId == null)
                return;

            await notificationClient.CreateNotificationAsync(notification => notification
                .WithRecipient(instance.AssignedPerson.AzureUniqueId)
                .WithTitle($"You were allocated as Contract Rep in contract '{position.Contract.Name} ({position.Contract.ContractNumber})'")
                .WithDescriptionMarkdown(NotificationDescription.PositionAssigned(position, instance)));
        }

        public async Task Handle(ExternalCompanyRepUpdated notification, CancellationToken cancellationToken)
        {
            var position = await orgApiClient.GetPositionV2Async(notification.PositionId);
            var instance = position?.GetActiveInstance();

            if (position == null || instance == null || instance.AssignedPerson == null || instance.AssignedPerson.AzureUniqueId == null)
                return;

            await notificationClient.CreateNotificationAsync(notification => notification
                .WithRecipient(instance.AssignedPerson.AzureUniqueId)
                .WithTitle($"You were allocated as External Company Rep in contract '{position.Contract.Name} ({position.Contract.ContractNumber})'")
                .WithDescriptionMarkdown(NotificationDescription.PositionAssigned(position, instance)));
        }

        public async Task Handle(ExternalContractRepUpdated notification, CancellationToken cancellationToken)
        {
            var position = await orgApiClient.GetPositionV2Async(notification.PositionId);
            var instance = position?.GetActiveInstance();

            if (position == null || instance == null || instance.AssignedPerson == null || instance.AssignedPerson.AzureUniqueId == null)
                return;

            await notificationClient.CreateNotificationAsync(notification => notification
                .WithRecipient(instance.AssignedPerson.AzureUniqueId)
                .WithTitle($"You were allocated as External Contract Rep in contract '{position.Contract.Name} ({position.Contract.ContractNumber})'")
                .WithDescriptionMarkdown(NotificationDescription.PositionAssigned(position, instance)));
        }

        private static class NotificationDescription
        {
            public static string DelegateRecertified(QueryDelegatedRole role) => new MarkdownDocument()
                .Paragraph($"{MdToken.Bold(role.RecertifiedBy?.Name)} ({role.RecertifiedBy?.Mail}) recertified your {role.Type} role in the contract '{role.Contract.Name}'.")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {role.Project.Name}")
                    .ListItem($"{MdToken.Bold("Contract:")} {role.Contract.ContractNumber}"))
                .Paragraph($"The role is **now valid** to **{role.ValidTo:dd/MM yyyy}**.")
                .Build();

            public static string DelegateAssigned(QueryDelegatedRole role) => new MarkdownDocument()
                .Paragraph($"{MdToken.Bold(role.CreatedBy.Name)} ({role.CreatedBy.Mail}) delegated you the {role.Type} role in the contract '{role.Contract.Name}'.")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {role.Project.Name}")
                    .ListItem($"{MdToken.Bold("Contract:")} {role.Contract.ContractNumber}"))
                .Paragraph($"The role is valid to **{role.ValidTo:dd/MM yyyy}**.")
                .Build();

            public static string PositionAssigned(ApiPositionV2 position, ApiPositionInstanceV2 instance) => new MarkdownDocument()
                .Paragraph($"You were assigned responsibility as '{position.BasePosition}'.")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {position.Project.Name}")
                    .ListItem($"{MdToken.Bold("Contract:")} {position.Contract.ContractNumber}")
                    .ListItem($"{MdToken.Bold("Position title:")} {position.Name}"))
                .Paragraph($"Position is active from **{instance.AppliesFrom:dd/MM yyyy}** to **{instance.AppliesTo:dd/MM yyyy}.")
                .Build();
        }
    }
}
