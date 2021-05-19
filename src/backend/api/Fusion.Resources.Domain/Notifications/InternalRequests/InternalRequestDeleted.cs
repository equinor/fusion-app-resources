using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.InternalRequests
{
    public class InternalRequestDeleted : INotification
    {
        public InternalRequestDeleted(Guid requestId, Guid orgProjectId, Guid? orgPositionId, Guid positionInstanceId, string type, string? subType)
        {
            RequestId = requestId;
            OrgProjectId = orgProjectId;
            OrgPositionId = orgPositionId;
            PositionInstanceId = positionInstanceId;
            Type = type;
            SubType = subType;
        }

        public Guid RequestId { get; set; }
        public Guid OrgProjectId { get; set; }
        public Guid? OrgPositionId { get; set; }
        public Guid PositionInstanceId { get; set; }
        public string Type { get; set; }
        public string? SubType { get; set; }
    }
}
