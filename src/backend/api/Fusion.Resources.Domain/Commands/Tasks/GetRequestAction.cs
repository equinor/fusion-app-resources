using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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
            private readonly IFusionProfileResolver profileResolver;

            public Handler(ResourcesDbContext db, IFusionProfileResolver profileResolver)
            {
                this.db = db;
                this.profileResolver = profileResolver;
            }
            public async Task<QueryRequestAction?> Handle(GetRequestAction request, CancellationToken cancellationToken)
            {
                var action = await db.RequestActions
                    .Include(t => t.AssignedTo)
                    .Include(t => t.ResolvedBy)
                    .Include(t => t.SentBy)
                    .SingleOrDefaultAsync(t => t.RequestId == request.requestId && t.Id == request.taskId, cancellationToken);

                return await action.AsQueryRequestActionAsync(profileResolver);
            }
        }
    }
}
