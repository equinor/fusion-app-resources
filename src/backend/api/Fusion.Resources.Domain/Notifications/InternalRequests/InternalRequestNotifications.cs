using System;
using MediatR;

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

        public class ProposedPerson : INotification
        {
            public ProposedPerson(Guid requestId)
            {
                RequestId = requestId;
            }

            public Guid RequestId { get; }
        }

        // TODO: Is this a good name?
        // Request is auto approved when the request was proposed without changes
        public class ProposedPersonAutoApproved : INotification
        {
            public ProposedPersonAutoApproved(Guid requestId)
            {
                RequestId = requestId;
            }

            public Guid RequestId { get; }
        }
    }
}