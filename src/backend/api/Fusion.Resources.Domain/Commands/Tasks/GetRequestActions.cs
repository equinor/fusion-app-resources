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
    public class GetRequestActions : IRequest<IEnumerable<QueryRequestAction>>
    {
        private Guid requestId;

        public GetRequestActions(Guid requestId)
        {
            this.requestId = requestId;
        }

        public class Handler : IRequestHandler<GetRequestActions, IEnumerable<QueryRequestAction>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task<IEnumerable<QueryRequestAction>> Handle(GetRequestActions request, CancellationToken cancellationToken)
            {
                var result = await db.RequestTasks
                    .Include(t => t.ResolvedBy)
                    .Where(t => t.RequestId == request.requestId)
                    .ToListAsync(cancellationToken);

                return result.Select(t => new QueryRequestAction(t));
            }
        }
    }
}
