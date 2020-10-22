using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.Request
{
    public class RequestApproved : INotification
    {
        public RequestApproved(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}
