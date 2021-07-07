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
    public class GetRequestTasks : IRequest<IEnumerable<QueryRequestTask>>
    {
        private Guid requestId;

        public GetRequestTasks(Guid requestId)
        {
            this.requestId = requestId;
        }

        public class Handler : IRequestHandler<GetRequestTasks, IEnumerable<QueryRequestTask>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task<IEnumerable<QueryRequestTask>> Handle(GetRequestTasks request, CancellationToken cancellationToken)
            {
                var result = await db.RequestTasks
                    .Include(t => t.ResolvedBy)
                    .Where(t => t.RequestId == request.requestId)
                    .ToListAsync(cancellationToken);

                return result.Select(t => new QueryRequestTask(t));
            }
        }
    }
}
