using System;
using MediatR;

namespace Fusion.Resources.Domain.Notifications
{
    public class ResourceAllocationRequestTaskOwnerAssigned : INotification
    {
        public ResourceAllocationRequestTaskOwnerAssigned(Guid requestId)
        {
            this.RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}