using Fusion.Resources.Database.Entities;
using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.Request
{
    public class RequestDeclinedByContractor : INotification
    {
        public RequestDeclinedByContractor(Guid requestId, string reason, DbPerson declinedBy)
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
