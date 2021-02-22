using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications
{
    public class InternalRequestCreated : INotification
    {
        public InternalRequestCreated(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}
