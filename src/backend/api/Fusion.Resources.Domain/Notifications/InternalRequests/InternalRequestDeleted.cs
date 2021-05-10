using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.InternalRequests
{
    public class InternalRequestDeleted : INotification
    {
        public InternalRequestDeleted(Guid requestId, Guid orgProjectId, Guid? orgPositionId, Guid positionInstanceId)
        {
            RequestId = requestId;
            OrgProjectId = orgProjectId;
            OrgPositionId = orgPositionId;
            PositionInstanceId = positionInstanceId;
        }

        public Guid RequestId { get; set; }
        public Guid OrgProjectId { get; }
        public Guid? OrgPositionId { get; }
        public Guid PositionInstanceId { get; }
    }
}
