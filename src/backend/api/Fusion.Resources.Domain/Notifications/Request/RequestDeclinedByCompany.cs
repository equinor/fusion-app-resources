using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.Request
{
    public class RequestDeclinedByCompany : INotification
    {
        public RequestDeclinedByCompany(Guid requestId, string reason)
        {
            RequestId = requestId;
            Reason = reason;
        }

        public Guid RequestId { get; }
        public string Reason { get; }
    }
}
