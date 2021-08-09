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
    public class GetMultipleRequestTasks : IRequest<ILookup<Guid, QueryRequestTask>>
    {
        private IEnumerable<Guid> requestId;

        public GetMultipleRequestTasks(IEnumerable<Guid> requestId)
        {
            this.requestId = requestId;
        }

        public class Handler : IRequestHandler<GetMultipleRequestTasks, ILookup<Guid, QueryRequestTask>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task<ILookup<Guid, QueryRequestTask>> Handle(GetMultipleRequestTasks request, CancellationToken cancellationToken)
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
