using System;
using MediatR;

namespace Fusion.Resources.Domain.Notifications
{
    public class WorkflowChanged : INotification
    {
        public WorkflowChanged(Guid requestId)
        {
            this.RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}