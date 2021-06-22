using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.InternalRequests
{
    public class InternalRequestAssignedDepartment : INotification
    {
        public InternalRequestAssignedDepartment(Guid requestId,string assignedDepartment)
        {
            RequestId = requestId;
            AssignedDepartment = assignedDepartment;
        }

        public Guid RequestId { get; }
        public string AssignedDepartment { get; }
    }
}
