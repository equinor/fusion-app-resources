using Fusion.AspNetCore.OData;
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
    public class GetRequestActions : IRequest<IEnumerable<QueryRequestAction>>
    {
        private Guid requestId;
        private ODataQueryParams? query;
        private DbTaskResponsible responsible;

        public GetRequestActions(Guid requestId, QueryTaskResponsible responsible)
        {
            this.requestId = requestId;
            this.responsible = responsible.MapToDatabase();
        }

        public GetRequestActions WithQuery(ODataQueryParams query)
        {
            this.query = query;
            return this;
        }

        public class Handler : IRequestHandler<GetRequestActions, IEnumerable<QueryRequestAction>>
        {
            private readonly ResourcesDbContext db;
            private readonly IFusionProfileResolver profileResolver;

            public Handler(ResourcesDbContext db, IFusionProfileResolver profileResolver)
            {
                this.db = db;
                this.profileResolver = profileResolver;
            }
            public async Task<IEnumerable<QueryRequestAction>> Handle(GetRequestActions request, CancellationToken cancellationToken)
            {
                var query = db.RequestActions
                    .Include(t => t.AssignedTo)
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

                // Filter 
                query = query.Where(t => t.Responsible == request.responsible || t.Responsible == DbTaskResponsible.Both);

                var result = await query.ToListAsync(cancellationToken);

                return await result.AsQueryRequestActionsAsync(profileResolver);
            }
        }
    }
}
