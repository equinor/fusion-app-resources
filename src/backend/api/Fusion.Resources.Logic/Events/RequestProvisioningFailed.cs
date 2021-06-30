using MediatR;
using System;

namespace Fusion.Resources.Logic.Events
{
    public class RequestProvisioningFailed : INotification
    {
        public RequestProvisioningFailed(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}
