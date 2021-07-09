using MediatR;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Fusion.Resources.Domain.Notifications.InternalRequests
{
    public class InternalRequestCreated : INotification
    {
        public InternalRequestCreated(Guid requestId, IEnumerable<PropertyEntry> modifiedProperties)
        {
            RequestId = requestId;
            ModifiedProperties = modifiedProperties;
        }

        public Guid RequestId { get; set; }
        public IEnumerable<PropertyEntry> ModifiedProperties { get; }
    }
}
