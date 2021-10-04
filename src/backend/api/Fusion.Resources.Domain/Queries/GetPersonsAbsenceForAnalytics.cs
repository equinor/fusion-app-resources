using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Diagnostics;
using Fusion.Resources.Domain.Queries;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Domain
{
    public class GetPersonsAbsenceForAnalytics : IRequest<QueryRangedList<QueryPersonAbsenceBasic>>
    {

        public GetPersonsAbsenceForAnalytics(ODataQueryParams query)
        {
            this.Query = query;
        }
        public ODataQueryParams Query { get; }

        public class Handler : IRequestHandler<GetPersonsAbsenceForAnalytics, QueryRangedList<QueryPersonAbsenceBasic>>
        {
            private readonly ResourcesDbContext db;
            private readonly IFusionLogger<GetPersonsAbsenceForAnalytics> log;

            public Handler(ResourcesDbContext db,IFusionLogger<GetPersonsAbsenceForAnalytics> log)
            {
                this.db = db;
                this.log = log;
            }

            public async Task<QueryRangedList<QueryPersonAbsenceBasic>> Handle(GetPersonsAbsenceForAnalytics request, CancellationToken cancellationToken)
            {
                var query = db.PersonAbsences
                    .Include(x => x.Person)
                    .Include(x => x.CreatedBy)
                    .Include(x => x.TaskDetails)
                    .AsQueryable();


                if (request.Query.HasFilter)
                {
                    query = query.ApplyODataFilters(request.Query, m =>
                    {
                        m.MapField("person.azureUniqueId", i => i.Person.AzureUniqueId);
                    });
                }

                var totalCount = await query.CountAsync(cancellationToken);

                var skip = request.Query.Skip.GetValueOrDefault(0);
                var take = request.Query.Top.GetValueOrDefault(totalCount);

                var pagedQuery = await QueryRangedList.FromQueryAsync(query.Select(x => new QueryPersonAbsenceBasic(x)), skip, take);

                log.LogTrace($"Analytics query executed");

                return pagedQuery;
            }
        }
    }
}