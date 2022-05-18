using Fusion.Resources.Database;
using Fusion.Resources.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Requests.Sharing
{
    public class RevokeShareRequest : IRequest<QuerySharedRequest?>
    {
        public RevokeShareRequest(Guid requestId, QueryPerson person, string source)
        {
            RequestId = requestId;
            Person = person;
            Source = source;
        }

        public Guid RequestId { get; }
        public QueryPerson Person { get; }
        public string Source { get; }

        public class Handler : IRequestHandler<RevokeShareRequest, QuerySharedRequest?>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<QuerySharedRequest?> Handle(RevokeShareRequest request, CancellationToken cancellationToken)
            {
                var sharedRequest = await db.SharedRequests.FirstOrDefaultAsync(x => 
                    x.RequestId == request.RequestId
                    && x.SharedWithId == request.Person.Id
                    && x.Source == request.Source, cancellationToken
                );
                if (sharedRequest is null) return null;

                sharedRequest.IsRevoked = true;
                sharedRequest.RevokedAt = DateTimeOffset.Now;

                await db.SaveChangesAsync(cancellationToken);

                return new QuerySharedRequest(sharedRequest);
            }
        }
    }
}
