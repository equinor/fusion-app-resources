using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Fusion.Resources.Domain.Models;

namespace Fusion.Resources.Domain;

public class GetPersonsAbsenceForAnalyticsStream : IRequest<PagedStreamResult<QueryPersonAbsenceBasic>>
{

    public GetPersonsAbsenceForAnalyticsStream(ODataQueryParams query)
    {
        this.Query = query;
    }
    public ODataQueryParams Query { get; }

    public class Handler : IRequestHandler<GetPersonsAbsenceForAnalyticsStream, PagedStreamResult<QueryPersonAbsenceBasic>>
    {
        private readonly ResourcesDbContext db;
        private readonly IFusionLogger<GetPersonsAbsenceForAnalyticsStream> log;

        public Handler(ResourcesDbContext db, IFusionLogger<GetPersonsAbsenceForAnalyticsStream> log)
        {
            this.db = db;
            this.log = log;
        }

        public async Task<PagedStreamResult<QueryPersonAbsenceBasic>> Handle(GetPersonsAbsenceForAnalyticsStream request, CancellationToken cancellationToken)
        {
            var query = db.PersonAbsences
                .Include(x => x.Person)
                .Include(x => x.TaskDetails)
                .OrderBy(x => x.Id)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken);
            if (totalCount == 0)
                totalCount = 100; // Default to a reasonable value

            var skip = request.Query.Skip.GetValueOrDefault(0);
            var take = request.Query.Top.GetValueOrDefault(totalCount);

            var pagedQuery = query
                .Select(x => new QueryPersonAbsenceBasic(x))
                .Skip(skip)
                .Take(take)
                .AsAsyncEnumerable();

            log.LogTrace($"Analytics streaming executed with total count: {totalCount}, Skip: {skip}, Top: {take}");

            return new PagedStreamResult<QueryPersonAbsenceBasic>(totalCount, take, skip, pagedQuery);
        }

    }
}