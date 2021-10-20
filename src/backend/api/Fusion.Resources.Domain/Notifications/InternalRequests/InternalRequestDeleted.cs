using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.InternalRequests
{
    public class InternalRequestDeleted : INotification
    {
        public InternalRequestDeleted(Guid requestId, Guid orgProjectId, Guid? orgPositionId, Guid positionInstanceId,
            string type, string? subType, long requestNumber, string? assignedDepartment, string removedByPerson)
        {
            RequestId = requestId;
            OrgProjectId = orgProjectId;
            OrgPositionId = orgPositionId;
            PositionInstanceId = positionInstanceId;
            Type = type;
            SubType = subType;
            RequestNumber = requestNumber;
            AssignedDepartment = assignedDepartment;
            RemovedByPerson = removedByPerson;
        }

        public Guid RequestId { get; set; }
        public Guid OrgProjectId { get; set; }
        public Guid? OrgPositionId { get; set; }
        public Guid PositionInstanceId { get; set; }
        public string Type { get; set; }
        public string? SubType { get; set; }
        public long RequestNumber { get; set; }
        public string? AssignedDepartment { get; set; }
        public string RemovedByPerson { get; set; }
    }
}
