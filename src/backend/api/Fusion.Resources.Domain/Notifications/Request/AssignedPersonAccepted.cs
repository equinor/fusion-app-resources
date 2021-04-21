using System;
using MediatR;

namespace Fusion.Resources.Domain.Notifications
{
    public class AssignedPersonAccepted : INotification
    {
        public AssignedPersonAccepted(Guid requestId)
        {
            this.RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}