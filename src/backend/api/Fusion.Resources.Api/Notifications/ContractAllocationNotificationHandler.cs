﻿using Fusion.ApiClients.Org;
using Fusion.Integration.Notification;
using Fusion.Resources.Api.Notifications.Markdown;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Notifications;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Notifications
{
    public class ContractAllocationNotificationHandler :
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

        public ContractAllocationNotificationHandler(IMediator mediator, IFusionNotificationClient notificationClient, IOrgApiClientFactory apiClientFactory)
        {
            this.mediator = mediator;
            this.notificationClient = notificationClient;
            this.orgApiClient = apiClientFactory.CreateClient(ApiClientMode.Application);
        }

        public async Task Handle(ContractRoleDelegated notification, CancellationToken cancellationToken)
        {
            var delegatedRole = await mediator.Send(new GetContractDelegatedRole(notification.RoleId));

            if (delegatedRole == null)
                return;

            await notificationClient.CreateNotificationForUserAsync(delegatedRole.Person.AzureUniqueId,
                $"You were delegated {delegatedRole.Type} role in {delegatedRole.Contract.ContractNumber} - {delegatedRole.Project.Name}",
                builder =>
                {
                    builder.AddDescription(NotificationDescription.DelegateAssigned(delegatedRole));
                });
        }

        public async Task Handle(DelegatedContractRoleRecertified notification, CancellationToken cancellationToken)
        {
            var delegatedRole = await mediator.Send(new GetContractDelegatedRole(notification.RoleId));

            if (delegatedRole == null)
                return;

            await notificationClient.CreateNotificationForUserAsync(delegatedRole.Person.AzureUniqueId,
                $"Your delegated role in {delegatedRole.Contract.ContractNumber} - {delegatedRole.Project.Name} was recertified",
                builder =>
                {
                    builder.AddDescription(NotificationDescription.DelegateRecertified(delegatedRole));
                });
        }

        public async Task Handle(CompanyRepUpdated notification, CancellationToken cancellationToken)
        {
            await CreateNotificationForPositionAsync(notification.PositionId, "You were allocated as Company Rep");
        }

        public async Task Handle(ContractRepUpdated notification, CancellationToken cancellationToken)
        {
            await CreateNotificationForPositionAsync(notification.PositionId, "You were allocated as Contract Rep");
        }

        public async Task Handle(ExternalCompanyRepUpdated notification, CancellationToken cancellationToken)
        {
            await CreateNotificationForPositionAsync(notification.PositionId, "You were allocated as External Company Rep");
        }

        public async Task Handle(ExternalContractRepUpdated notification, CancellationToken cancellationToken)
        {
            await CreateNotificationForPositionAsync(notification.PositionId, "You were allocated as External Contract Rep");
        }

        private async Task CreateNotificationForPositionAsync(Guid positionId, string title)
        {
            var position = await orgApiClient.GetPositionV2Async(positionId);
            var instance = position?.GetActiveInstance();

            if (position == null || instance?.AssignedPerson?.AzureUniqueId == null)
                return;

            await notificationClient.CreateNotificationForUserAsync(instance.AssignedPerson.AzureUniqueId, title, builder =>
                {
                    builder.AddDescription(NotificationDescription.PositionAssigned(position, instance));
                });
        }

        private static class NotificationDescription
        {
            public static string DelegateRecertified(QueryDelegatedRole role) => new MarkdownDocument()
                .Paragraph($"{MdToken.Bold(role.RecertifiedBy?.Name)} ({role.RecertifiedBy?.Mail}) recertified your {role.Type} role.")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {role.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {role.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {role.Contract?.ContractNumber}"))
                .Paragraph($"The role is **now valid** to **{role.ValidTo:dd/MM yyyy}**.")
                .Build();

            public static string DelegateAssigned(QueryDelegatedRole role) => new MarkdownDocument()
                .Paragraph($"{MdToken.Bold(role.CreatedBy.Name)} ({role.CreatedBy.Mail}) delegated you the {role.Type} role.")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {role.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {role.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {role.Contract?.ContractNumber}"))
                .Paragraph($"The role is valid to **{role.ValidTo:dd/MM yyyy}**.")
                .Build();

            public static string PositionAssigned(ApiPositionV2 position, ApiPositionInstanceV2 instance) => new MarkdownDocument()
                .Paragraph($"You were assigned responsibility as {position.Name}.")
                .List(l => l
                    .ListItem($"{MdToken.Bold("Project:")} {position.Project?.Name}")
                    .ListItem($"{MdToken.Bold("Contract name:")} {position.Contract?.Name}")
                    .ListItem($"{MdToken.Bold("Contract number:")} {position.Contract?.ContractNumber}"))
                .Paragraph($"Position is active from **{instance.AppliesFrom:dd/MM yyyy}** to **{instance.AppliesTo:dd/MM yyyy}**.")
                .Build();
        }
    }
}
