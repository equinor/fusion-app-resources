using Fusion.Resources.Database.Entities;
using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.Request
{
    public class RequestDeclinedByCompany : INotification
    {
        public RequestDeclinedByCompany(Guid requestId, string reason, DbPerson declinedBy)
        {
            RequestId = requestId;
            Reason = reason;
            DeclinedBy = declinedBy;
        }

        public Guid RequestId { get; }

        public string Reason { get; }
        public DbPerson DeclinedBy { get; }
    }
}
