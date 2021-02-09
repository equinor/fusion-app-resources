using Fusion.Resources.Database.Entities;
using MediatR;
using System;

namespace Fusion.Resources.Domain.Notifications.Request
{
    public class RequestProposedByCompany : INotification
    {
        public RequestProposedByCompany(Guid requestId, DbPerson proposedBy)
        {
            RequestId = requestId;
            ProposedBy = proposedBy;
        }

        public Guid RequestId { get; }

        public DbPerson ProposedBy { get; }
    }
}
