using System;
using MediatR;

namespace Fusion.Resources.Domain.Notifications
{
    public class AllocatedPersonProposal : INotification
    {
        public AllocatedPersonProposal(Guid requestId)
        {
            this.RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}