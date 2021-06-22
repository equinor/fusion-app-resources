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
        public InternalRequestUpdated(Guid requestId, Guid initiatedByDbPersonId, IEnumerable<PropertyEntry> requestProperties)
        {
            RequestId = requestId;
            InitiatedByDbPersonId = initiatedByDbPersonId;
            ModifiedProperties = requestProperties.Where(x => x.IsModified);

        }

        public Guid RequestId { get; }
        public IEnumerable<PropertyEntry> ModifiedProperties { get; }
        public Guid InitiatedByDbPersonId { get; }
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
            var assignedDepartmentModified = notification.ModifiedProperties.FirstOrDefault(x => x.EntityEntry.Metadata.Name == nameof(DbResourceAllocationRequest.AssignedDepartment));

            if (assignedDepartmentModified != null)
            {
                await mediator.Publish(new InternalRequestAssignedDepartment(notification.RequestId, notification.InitiatedByDbPersonId, $"{assignedDepartmentModified.CurrentValue}"));
            }
        }
    }
}
