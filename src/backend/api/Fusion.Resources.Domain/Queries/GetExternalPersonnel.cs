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
    public class GetExternalPersonnel : IRequest<IEnumerable<QueryExternalPersonnelPerson>>
    {
        public GetExternalPersonnel(ODataQueryParams? queryParams = null)
        {
            Query = queryParams ?? new ODataQueryParams(); //avoiding som null-checking in handler.
        }

        public ODataQueryParams Query { get; set; }

        public class Handler : IRequestHandler<GetExternalPersonnel, IEnumerable<QueryExternalPersonnelPerson>>
        {
            private readonly ResourcesDbContext db;

            public Handler(ResourcesDbContext db)
            {
                this.db = db;
            }

            public async Task<IEnumerable<QueryExternalPersonnelPerson>> Handle(GetExternalPersonnel request, CancellationToken cancellationToken)
            {
                var query = db.ExternalPersonnel.AsQueryable();

                if (request.Query.HasFilter)
                {
                    query = query.ApplyODataFilters(request.Query, mapper =>
                    {
                        mapper.MapField("azureAdStatus", p => p.AccountStatus);
                        mapper.MapField("name", p => p.Name);
                        mapper.MapField("phoneNumber", p => p.Phone);
                    });
                }

                query = query.OrderBy(ep => ep.Id); //paging requires consistent ordering, use default id for now.

                if (request.Query.Skip.HasValue) query = query.Skip(request.Query.Skip.Value);
                if (request.Query.Top.HasValue) query = query.Take(request.Query.Top.Value);

                var matches = await query.Include(ep => ep.Disciplines).ToListAsync();

                return matches.Select(ep => new QueryExternalPersonnelPerson(ep)).ToList();
            }
        }
    }
}
