using Fusion.ApiClients.Org;
using Fusion.Integration;
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
    public class CountActionsForRequests : IRequest<IDictionary<Guid, int>>
    {
        private IEnumerable<Guid> requestId;

        public CountActionsForRequests(IEnumerable<Guid> requestId)
        {
            this.requestId = requestId;
        }

        public class Handler : IRequestHandler<CountActionsForRequests, IDictionary<Guid, int>>
        {
            private readonly ResourcesDbContext db;
            private readonly IFusionProfileResolver profileResolver;

            public Handler(ResourcesDbContext db, IFusionProfileResolver profileResolver)
            {
                this.db = db;
                this.profileResolver = profileResolver;
            }

            public async Task<IDictionary<Guid, int>> Handle(CountActionsForRequests request, CancellationToken cancellationToken)
            {
                return await db.RequestActions
                    .Where(t => request.requestId.Contains(t.RequestId))
                    .GroupBy(t => t.RequestId)
                    .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
            }
        }
    }
}
