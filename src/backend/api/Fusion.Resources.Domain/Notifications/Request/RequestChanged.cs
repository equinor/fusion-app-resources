using System;
using MediatR;

namespace Fusion.Resources.Domain.Notifications
{
    public class RequestChanged : INotification
    {
        public RequestChanged(Guid requestId)
        {
            this.RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}