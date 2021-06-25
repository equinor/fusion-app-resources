using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Fusion.Resources.Domain.Notifications.InternalRequests
{
    public class InternalRequestUpdated : INotification
    {
        public InternalRequestUpdated(Guid requestId, IEnumerable<PropertyEntry> modifiedProperties)
        {
            RequestId = requestId;
            ModifiedProperties = modifiedProperties;
        }

        public Guid RequestId { get; }
        public IEnumerable<PropertyEntry> ModifiedProperties { get; }
    }

    public class InternalRequestUpdatedHandler : INotificationHandler<InternalRequestUpdated>
    {
        private readonly IMediator mediator;

        public InternalRequestUpdatedHandler(IMediator mediator)
        {
            this.mediator = mediator;
        }
        public async Task Handle(InternalRequestUpdated notification, CancellationToken cancellationToken)
        {
            var assignedDepartmentModified = notification.ModifiedProperties.Any(x => x.Metadata.Name == nameof(DbResourceAllocationRequest.AssignedDepartment));

            if (assignedDepartmentModified)
            {
                await mediator.Publish(new InternalRequestNotifications.AssignedDepartment(notification.RequestId));
            }
        }
    }
}
