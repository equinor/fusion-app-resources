using Fusion.ApiClients.Org;
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
    public class CountActionsForRequests : IRequest<IDictionary<Guid, int>>
    {
        private readonly Guid[] requestId;
        private readonly DbTaskResponsible responsible;

        public CountActionsForRequests(IEnumerable<Guid> requestId, QueryTaskResponsible responsible)
        {
            this.requestId = requestId.ToArray();
            this.responsible = responsible.MapToDatabase();
        }

        public class Handler : IRequestHandler<CountActionsForRequests, IDictionary<Guid, int>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<IDictionary<Guid, int>> Handle(CountActionsForRequests request, CancellationToken cancellationToken)
            {
                return await db.RequestActions
                    .Where(t => request.requestId.Contains(t.RequestId) && t.Responsible == request.responsible || t.Responsible == DbTaskResponsible.Both)
                    .GroupBy(t => t.RequestId)
                    .Select(g => new { g.Key, Count = g.Count() })
                    .ToDictionaryAsync(g => g.Key, g => g.Count, cancellationToken);
            }
        }
    }
}
