using Fusion.AspNetCore.OData;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetExternalPersonell : IRequest<IEnumerable<QueryExternalPersonnelPerson>>
    {
        public GetExternalPersonell(ODataQueryParams queryParams = null)
        {
            Query = queryParams;
        }

        public ODataQueryParams Query { get; set; }

        public class Handler : IRequestHandler<GetExternalPersonell, IEnumerable<QueryExternalPersonnelPerson>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<IEnumerable<QueryExternalPersonnelPerson>> Handle(GetExternalPersonell request, CancellationToken cancellationToken)
            {
                var query = db.ExternalPersonnel.AsQueryable();

                if (request.Query?.HasFilter ?? false)
                {
                    query = query.ApplyODataFilters(request.Query, mapper =>
                    {
                        mapper.MapField("azureAdStatus", p => p.AccountStatus);
                        mapper.MapField("name", p => p.Name);
                        mapper.MapField("phoneNumber", p => p.Phone);
                    });
                }

                var matches = await query.Include(ep => ep.Disciplines).ToListAsync();

                return matches.Select(ep => new QueryExternalPersonnelPerson(ep)).ToList();
            }
        }
    }
}
