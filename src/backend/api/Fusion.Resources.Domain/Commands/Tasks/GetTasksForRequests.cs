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
    public class GetTasksForRequests : IRequest<ILookup<Guid, QueryRequestTask>>
    {
        private IEnumerable<Guid> requestId;

        public GetTasksForRequests(IEnumerable<Guid> requestId)
        {
            this.requestId = requestId;
        }

        public class Handler : IRequestHandler<GetTasksForRequests, ILookup<Guid, QueryRequestTask>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task<ILookup<Guid, QueryRequestTask>> Handle(GetTasksForRequests request, CancellationToken cancellationToken)
            {
                var result = await db.RequestTasks
                    .Include(t => t.ResolvedBy)
                    .Where(t => request.requestId.Contains(t.RequestId))
                    .ToListAsync(cancellationToken);

                return result
                    .ToLookup(x => x.RequestId, x => new QueryRequestTask(x));
            }
        }
    }
}
