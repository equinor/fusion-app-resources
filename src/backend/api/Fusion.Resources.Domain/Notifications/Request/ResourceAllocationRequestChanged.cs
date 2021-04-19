using System;
using MediatR;

namespace Fusion.Resources.Domain.Notifications
{
    public class ResourceAllocationRequestChanged : INotification
    {
        public ResourceAllocationRequestChanged(Guid requestId)
        {
            this.RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}