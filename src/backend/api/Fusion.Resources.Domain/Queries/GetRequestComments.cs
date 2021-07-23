using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetRequestComments : IRequest<IEnumerable<QueryRequestComment>>
    {
        public GetRequestComments(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public class Handler : IRequestHandler<GetRequestComments, IEnumerable<QueryRequestComment>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<IEnumerable<QueryRequestComment>> Handle(GetRequestComments request, CancellationToken cancellationToken)
            {
                var comments = await db.RequestComments
                    .Include(rc => rc.CreatedBy)
                    .Include(rc => rc.UpdatedBy)
                    .Where(rc => rc.RequestId == request.RequestId).ToListAsync(cancellationToken);

                var results = comments.Select(c => new QueryRequestComment(c));

                return results;
            }
        }
    }
}
