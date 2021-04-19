using System;
using MediatR;

namespace Fusion.Resources.Domain.Notifications
{
    public class ResourceAllocationRequestAssignedPersonAccepted : INotification
    {
        public ResourceAllocationRequestAssignedPersonAccepted(Guid requestId)
        {
            this.RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}