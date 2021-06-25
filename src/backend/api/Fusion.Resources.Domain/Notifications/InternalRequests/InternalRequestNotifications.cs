using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.InternalRequests
{
    public partial class InternalRequestNotifications
    {
        public class AssignedDepartment : INotification
        {
            public AssignedDepartment(Guid requestId)
            {
                RequestId = requestId;
            }

            public Guid RequestId { get; }
        }

      
    }
}