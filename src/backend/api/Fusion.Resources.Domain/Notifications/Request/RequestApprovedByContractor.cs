using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.Request
{
    public class RequestApprovedByContractor : INotification
    {
        public RequestApprovedByContractor(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}
