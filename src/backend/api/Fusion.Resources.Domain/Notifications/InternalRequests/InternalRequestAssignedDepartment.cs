using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.InternalRequests
{
    public class InternalRequestAssignedDepartment : INotification
    {
        public InternalRequestAssignedDepartment(Guid requestId, Guid personDbId, string assignedDepartment)
        {
            RequestId = requestId;
            InitiatedByDbPersonId = personDbId;
            AssignedDepartment = assignedDepartment;
        }

        public Guid RequestId { get; }
        public Guid InitiatedByDbPersonId { get; }
        public string AssignedDepartment { get; }
    }
}
