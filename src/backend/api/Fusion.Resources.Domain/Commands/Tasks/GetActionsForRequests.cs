using Fusion.Integration;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
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
        private readonly DbTaskResponsible responsible;
        private readonly Guid[] requestId;

        public GetActionsForRequests(IEnumerable<Guid> requestId, QueryTaskResponsible responsible)
        {
            this.requestId = requestId.ToArray();
            this.responsible = responsible.MapToDatabase();
        }

        public class Handler : IRequestHandler<GetActionsForRequests, ILookup<Guid, QueryRequestAction>>
        {
            private readonly ResourcesDbContext db;
            private readonly IFusionProfileResolver profileResolver;

            public Handler(ResourcesDbContext db, IFusionProfileResolver profileResolver)
            {
                this.db = db;
                this.profileResolver = profileResolver;
            }

            public async Task<ILookup<Guid, QueryRequestAction>> Handle(GetActionsForRequests request, CancellationToken cancellationToken)
            {
                var result = await db.RequestActions
                    .Include(t => t.ResolvedBy)
                    .Include(t => t.AssignedTo)
                    .Include(t => t.SentBy)
                    .Where(t => request.requestId.Contains(t.RequestId))
                    .Where(t => t.Responsible == request.responsible || t.Responsible == DbTaskResponsible.Both)
                    .ToListAsync(cancellationToken);

                var actions = await result.AsQueryRequestActionsAsync(profileResolver);

                return actions.ToLookup(x => x.RequestId);
            }
        }
    }
}
