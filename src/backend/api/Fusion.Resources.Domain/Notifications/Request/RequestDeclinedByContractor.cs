using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.Request
{
    public class RequestDeclinedByContractor : INotification
    {
        public RequestDeclinedByContractor(Guid requestId, string reason)
        {
            RequestId = requestId;
            Reason = reason;
        }

        public Guid RequestId { get; }
        public string Reason { get; }
    }
}
