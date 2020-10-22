using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.Request
{
    public class RequestApprovedByCompany : INotification
    {
        public RequestApprovedByCompany(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}
