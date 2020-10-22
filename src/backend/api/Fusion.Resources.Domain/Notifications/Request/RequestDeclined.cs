using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.Request
{
    public class RequestDeclined : INotification
    {
        public RequestDeclined(Guid requestId, string reason)
        {
            RequestId = requestId;
            Reason = reason;
        }

        public Guid RequestId { get; }
        public string Reason { get; }
    }
}
