using System;
using MediatR;

namespace Fusion.Resources.Domain.Notifications
{
    public class ResourceAllocationRequestAllocatedPersonProposal : INotification
    {
        public ResourceAllocationRequestAllocatedPersonProposal(Guid requestId)
        {
            this.RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}