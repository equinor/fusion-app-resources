using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Notification;
using Fusion.Integration.Org;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Notifications.InternalRequests;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Api.Notifications
{

    public partial class InternalRequestNotification
    {
        public class RequestDeletedHandler : INotificationHandler<InternalRequestDeleted>
        {
            private readonly IMediator mediator;
            private readonly INotificationBuilder notificationBuilder;
            private readonly IProjectOrgResolver orgResolver;
            private readonly ILogger<InternalRequestNotification> logger;

            public RequestDeletedHandler(IMediator mediator, INotificationBuilderFactory notificationBuilderFactory, IProjectOrgResolver orgResolver, ILogger<InternalRequestNotification> logger)
            {
                this.mediator = mediator;
                this.notificationBuilder = notificationBuilderFactory.CreateDesigner();
                this.orgResolver = orgResolver;
                this.logger = logger;
            }

            public async Task Handle(InternalRequestDeleted notification, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(notification.AssignedDepartment))
                {
                    logger.LogInformation($"Request {notification.RequestId} was removed. Notification not sent due to missing assigned department.");
                    return;
                }

                var orgPosition = await orgResolver.ResolvePositionAsync(notification.OrgPositionId.GetValueOrDefault());
                var orgPositionInstance = orgPosition?.Instances.SingleOrDefault(x => x.Id == notification.PositionInstanceId);
                if (orgPositionInstance is null)
                {
                    logger.LogWarning($"Request {notification.RequestId} was removed. Notification not sent due to missing orgchart position instance.");
                    return;
                }

                try
                {
                    notificationBuilder
                        .AddTitle($"A {notification.SubType} personnel request has been deleted, and removed from your department")
                        .AddDescription($"Request was deleted by {notification.RemovedByPerson}")

                        .AddFacts(facts => facts
                            .AddFactIf("Request number", $"{notification.RequestNumber}", notification.RequestNumber > 0)
                            .AddFactIf("Project", orgPosition!.Project?.Name ?? "", orgPosition!.Project is not null)
                            .AddFactIf("Position id", orgPosition!.ExternalId ?? "", orgPosition!.ExternalId is not null)
                            .AddFactIf("Position", orgPosition!.Name ?? "", orgPosition!.Name is not null)
                            .AddFact("Period", $"{orgPositionInstance.GetFormattedPeriodString()}")
                            .AddFact("Workload", $"{orgPositionInstance.GetFormattedWorkloadString()}")
                            .AddFact("Internal request id", $"{notification.RequestId}")
                        );

                    var card = await notificationBuilder.BuildCardAsync();
                    await mediator.Send(new NotifyResourceOwner(notification.AssignedDepartment, card, "Personnel request has been deleted"));

                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }
        }
    }
}