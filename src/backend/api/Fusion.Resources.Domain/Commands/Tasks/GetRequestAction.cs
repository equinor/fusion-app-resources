using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Tasks
{
    public class GetRequestAction : IRequest<QueryRequestAction?>
    {
        private Guid requestId;
        private Guid taskId;

        public GetRequestAction(Guid requestId, Guid taskId)
        {
            this.requestId = requestId;
            this.taskId = taskId;
        }

        public class Handler : IRequestHandler<GetRequestAction, QueryRequestAction?>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task<QueryRequestAction?> Handle(GetRequestAction request, CancellationToken cancellationToken)
            {
                var task = await db.RequestTasks
                    .Include(t => t.ResolvedBy)
                    .Include(t => t.SentBy)
                    .SingleOrDefaultAsync(t => t.RequestId == request.requestId && t.Id == request.taskId, cancellationToken);

                return task is not null ? new QueryRequestAction(task) : null;
            }
        }
    }
}
