using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.InternalRequests
{
    public class InternalRequestDeleted : INotification
    {
        public InternalRequestDeleted(QueryResourceAllocationRequest req, string editor)
        {
            RequestId = req.RequestId;
            IsDraft = req.IsDraft;
            OrgProjectId = req.Project.OrgProjectId;
            OrgPositionId = req.OrgPositionId;
            PositionInstanceId = req.OrgPositionInstanceId;
            Type = $"{req.Type}";
            SubType = req.SubType;
            RequestNumber = req.RequestNumber;
            AssignedDepartment = req.AssignedDepartment;
            RemovedByPerson = editor;
        }

        public Guid RequestId { get; set; }
        public bool IsDraft { get; set; }
        public Guid OrgProjectId { get; set; }
        public Guid? OrgPositionId { get; set; }
        public Guid? PositionInstanceId { get; set; }
        public string Type { get; set; }
        public string? SubType { get; set; }
        public long RequestNumber { get; set; }
        public string? AssignedDepartment { get; set; }
        public string RemovedByPerson { get; set; }
    }
}
