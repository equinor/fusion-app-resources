using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications
{
    public class RequestCreated : INotification
    {
        public RequestCreated(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}
