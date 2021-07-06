using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Tasks
{
    public class GetRequestTask : IRequest<QueryRequestTask?>
    {
        private Guid requestId;
        private Guid taskId;

        public GetRequestTask(Guid requestId, Guid taskId)
        {
            this.requestId = requestId;
            this.taskId = taskId;
        }

        public class Handler : IRequestHandler<GetRequestTask, QueryRequestTask?>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task<QueryRequestTask?> Handle(GetRequestTask request, CancellationToken cancellationToken)
            {
                var task = await db.RequestTasks
                    .Include(t => t.ResolvedBy)
                    .SingleOrDefaultAsync(t => t.RequestId == request.requestId && t.Id == request.taskId, cancellationToken);

                return task is not null ? new QueryRequestTask(task) : null;
            }
        }
    }
}
