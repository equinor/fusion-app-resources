using System;
using MediatR;

namespace Fusion.Resources.Domain.Notifications
{
    public class ProposedPersonChanged : INotification
    {
        public ProposedPersonChanged(Guid requestId)
        {
            this.RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}