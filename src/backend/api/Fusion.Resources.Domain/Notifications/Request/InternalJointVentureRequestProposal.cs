using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.Request
{
    public class InternalJointVentureRequestProposal : INotification
    {
        public InternalJointVentureRequestProposal(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}
