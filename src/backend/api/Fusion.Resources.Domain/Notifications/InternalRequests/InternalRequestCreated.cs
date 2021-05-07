using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Notifications.InternalRequests
{
    public class InternalRequestCreated : INotification
    {
        public InternalRequestCreated(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; set; }
    }
}
