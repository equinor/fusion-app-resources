using MediatR;
using System;

namespace Fusion.Resources.Logic.Events
{
    public class RequestProvisioned : INotification
    {
        public RequestProvisioned(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}
