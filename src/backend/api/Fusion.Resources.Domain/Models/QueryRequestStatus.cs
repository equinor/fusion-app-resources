using System;

namespace Fusion.Resources.Domain.Models
{
    public class QueryRequestStatus
    {
        public QueryRequestStatus(QueryResourceAllocationRequest changeRequest)
        {
            Id = changeRequest.RequestId;
            State = changeRequest.State;
            IsDraft = changeRequest.IsDraft;
        }

        public Guid Id { get; }
        public string? State { get; }
        public bool IsDraft { get; } = false;
    }
}
