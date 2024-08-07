﻿using MediatR;
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

        public class ProposedPerson : INotification
        {
            public ProposedPerson(Guid requestId)
            {
                RequestId = requestId;
            }

            public Guid RequestId { get; }
        }


        /// <summary>
        ///     Sent when a request is auto approved because the request was proposed without changes
        /// </summary>
        public class ProposedPersonAutoAccepted : INotification
        {
            public ProposedPersonAutoAccepted(Guid requestId)
            {
                RequestId = requestId;
            }

            public Guid RequestId { get; }
        }
    }
}