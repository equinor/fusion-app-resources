using Fusion.Resources.Database.Entities;
using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.Request
{
    public class RequestApprovedByCompany : INotification
    {
        public RequestApprovedByCompany(Guid requestId, DbPerson approvedBy)
        {
            RequestId = requestId;
            ApprovedBy = approvedBy;
        }

        public Guid RequestId { get; }

        public DbPerson ApprovedBy { get; }
    }
}
