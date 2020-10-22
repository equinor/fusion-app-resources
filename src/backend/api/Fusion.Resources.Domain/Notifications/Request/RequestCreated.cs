using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications
{
    public class RequestCreated : INotification
    {
        public RequestCreated(Guid requestId, PersonId approver)
        {
            RequestId = requestId;
            Approver = approver;
        }

        public Guid RequestId { get; }
        public PersonId Approver { get; }
    }
}
