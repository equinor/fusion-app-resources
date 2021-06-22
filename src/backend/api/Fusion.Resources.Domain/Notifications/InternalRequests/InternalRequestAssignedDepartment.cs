using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.InternalRequests
{
    public class InternalRequestAssignedDepartment : INotification
    {
        public InternalRequestAssignedDepartment(Guid requestId, Guid initiatedByDbPersonId, string assignedDepartment)
        {
            RequestId = requestId;
            InitiatedByDbPersonId = initiatedByDbPersonId;
            AssignedDepartment = assignedDepartment;
        }

        public Guid RequestId { get; }
        public Guid InitiatedByDbPersonId { get; }
        public string AssignedDepartment { get; }
    }
}
