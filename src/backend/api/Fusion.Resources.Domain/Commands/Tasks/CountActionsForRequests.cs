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

namespace Fusion.Resources.Domain
{
    /// <summary>
    /// Count resolved, unresolved, and total tasks per recipient for given request ids without 
    /// retrieving all actions from the database.
    /// </summary>
    public class CountActionsForRequests : IRequest<IDictionary<Guid, QueryActionCounts>>
    {
        private readonly Guid[] requestId;
        private readonly DbTaskResponsible responsible;

        public CountActionsForRequests(IEnumerable<Guid> requestId, QueryTaskResponsible responsible)
        {
            this.requestId = requestId.ToArray();
            this.responsible = responsible.MapToDatabase();
        }

        public class Handler : IRequestHandler<CountActionsForRequests, IDictionary<Guid, QueryActionCounts>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<IDictionary<Guid, QueryActionCounts>> Handle(CountActionsForRequests request, CancellationToken cancellationToken)
            {
                return await db.RequestActions
                    .Where(t => request.requestId.Contains(t.RequestId) && t.Responsible == request.responsible || t.Responsible == DbTaskResponsible.Both)
                    .GroupBy(t => t.RequestId)
                    .Select(g => new { 
                        g.Key, 
                        ResolvedCount = g.Count(x => x.IsResolved), 
                        UnresolvedCount = g.Count(x => !x.IsResolved) 
                    })
                    .ToDictionaryAsync(g => g.Key, g => new QueryActionCounts(g.ResolvedCount, g.UnresolvedCount), cancellationToken);
            }
        }
    }

    public record QueryActionCounts(int ResolvedCount, int UnresolvedCount)
    {
        public int TotalCount => ResolvedCount + UnresolvedCount;
    }
}
