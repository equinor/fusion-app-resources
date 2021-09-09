using Fusion.AspNetCore.OData;
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
        private ODataQueryParams? query;

        public GetRequestActions(Guid requestId)
        {
            this.requestId = requestId;
        }

        public GetRequestActions WithQuery(ODataQueryParams query)
        {
            this.query = query;
            return this;
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
                var query = db.RequestActions
                    .Include(t => t.ResolvedBy)
                    .Include(t => t.SentBy)
                    .Where(t => t.RequestId == request.requestId);

                if (request.query?.HasFilter == true)
                {
                    query = query.ApplyODataFilters(request.query, opts =>
                    {
                        opts.MapField("type", x => x.Type);
                        opts.MapField("source", x => x.Source);
                        opts.MapField("responsible", x => x.Responsible);
                    });
                }

                var result = await query.ToListAsync(cancellationToken);

                return result.Select(t => new QueryRequestAction(t));
            }
        }
    }
}
