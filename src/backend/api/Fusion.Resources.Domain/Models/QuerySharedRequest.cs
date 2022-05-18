using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Models
{
    public class QuerySharedRequest
    {
        public QuerySharedRequest(DbSharedRequest entity)
        {
            Id = entity.Id;

            RequestId = entity.RequestId;
            Request = new QueryResourceAllocationRequest(entity.Request);

            SharedWithId = entity.SharedWithId;
            SharedWith = new QueryPerson(entity.SharedWith);

            SharedById = entity.SharedById;
            SharedBy = new QueryPerson(entity.SharedBy);

            Scope = entity.Scope;
            Source = entity.Source;
            Reason = entity.Reason;

            IsRevoked = entity.IsRevoked;
            RevokedAt = entity.RevokedAt;
            GrantedAt = entity.GrantedAt;
        }

        public Guid Id { get; set; }

        public Guid RequestId { get; set; }
        public QueryResourceAllocationRequest Request { get; set; } = null!;

        public Guid SharedWithId { get; set; }
        public QueryPerson SharedWith { get; set; } = null!;

        public Guid SharedById { get; set; }
        public QueryPerson SharedBy { get; set; } = null!;

        public string Scope { get; set; } = null!;
        public string Source { get; set; } = null!;
        public string Reason { get; set; } = null!;

        public bool IsRevoked { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }

        public DateTimeOffset GrantedAt { get; set; }
    }
}
