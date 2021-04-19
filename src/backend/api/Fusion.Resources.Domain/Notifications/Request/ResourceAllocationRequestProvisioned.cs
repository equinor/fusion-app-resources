using System;
using MediatR;

namespace Fusion.Resources.Domain.Notifications
{
    public class ResourceAllocationRequestProvisioned : INotification
    {
        public ResourceAllocationRequestProvisioned(Guid requestId)
        {
            this.RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}