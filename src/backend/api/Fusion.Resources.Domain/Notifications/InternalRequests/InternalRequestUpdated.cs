using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.InternalRequests
{
    public class InternalRequestUpdated : INotification
    {
        public InternalRequestUpdated(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; set; }
    }
}
