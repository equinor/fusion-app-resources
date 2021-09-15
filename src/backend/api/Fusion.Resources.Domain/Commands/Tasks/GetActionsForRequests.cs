using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Tasks
{
    /// <summary>
    /// Get all tasks for multiple requests, specified with request ids.
    /// </summary>
    public class GetActionsForRequests : IRequest<ILookup<Guid, QueryRequestAction>>
    {
        private IEnumerable<Guid> requestId;

        public GetActionsForRequests(IEnumerable<Guid> requestId)
        {
            this.requestId = requestId;
        }

        public class Handler : IRequestHandler<GetActionsForRequests, ILookup<Guid, QueryRequestAction>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task<ILookup<Guid, QueryRequestAction>> Handle(GetActionsForRequests request, CancellationToken cancellationToken)
            {
                var result = await db.RequestActions
                    .Include(t => t.ResolvedBy)
                    .Include(t => t.AssignedTo)
                    .Include(t => t.SentBy)
                    .Where(t => request.requestId.Contains(t.RequestId))
                    .ToListAsync(cancellationToken);

                return result
                    .ToLookup(x => x.RequestId, x => new QueryRequestAction(x));
            }
        }
    }
}
