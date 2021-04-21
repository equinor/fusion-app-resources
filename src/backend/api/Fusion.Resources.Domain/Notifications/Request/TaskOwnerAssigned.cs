using System;
using MediatR;

namespace Fusion.Resources.Domain.Notifications
{
    public class TaskOwnerAssigned : INotification
    {
        public TaskOwnerAssigned(Guid requestId)
        {
            this.RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}