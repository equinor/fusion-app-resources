using Fusion.Resources.Database;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Fusion.Resources.Domain.Queries;
public class PagedAsyncResult<T>
{
    public int TotalCount { get; }
    public IAsyncEnumerable<T> Items { get; }

    public PagedAsyncResult(int totalCount, IAsyncEnumerable<T> items)
    {
        TotalCount = totalCount;
        Items = items;
    }
}


public class GetResourceAllocationRequestsForAnalyticsStream : IRequest<PagedAsyncResult<QueryResourceAllocationRequest>>
{
    public GetResourceAllocationRequestsForAnalyticsStream(ODataQueryParams query)
    {
        this.Query = query;
    }
    public ODataQueryParams Query { get; }


    public class Handler : IRequestHandler<GetResourceAllocationRequestsForAnalyticsStream, PagedAsyncResult<QueryResourceAllocationRequest>>
    {
        private readonly ResourcesDbContext db;
        private readonly IFusionLogger<GetResourceAllocationRequestsForAnalyticsStream> log;

        public Handler(ResourcesDbContext db, IFusionLogger<GetResourceAllocationRequestsForAnalyticsStream> log)
        {
            this.db = db;
            this.log = log;
        }

        public async Task<PagedAsyncResult<QueryResourceAllocationRequest>> Handle(GetResourceAllocationRequestsForAnalyticsStream request, CancellationToken cancellationToken)
        {
            var query = db.ResourceAllocationRequests
                .Include(r => r.OrgPositionInstance)
                .Include(r => r.CreatedBy)
                .Include(r => r.UpdatedBy)
                .Include(r => r.Project)
                .Include(r => r.ProposedPerson)
                .OrderBy(x => x.Id)
                .AsQueryable();

            if (request.Query.HasFilter)
            {
                query = query.ApplyODataFilters(request.Query, m =>
                {
                    m.MapField(nameof(QueryResourceAllocationRequest.AssignedDepartment), i => i.AssignedDepartment);
                    m.MapField(nameof(QueryResourceAllocationRequest.Discipline), i => i.Discipline);
                    m.MapField("isDraft", i => i.IsDraft);
                    m.MapField("project.id", i => i.Project.OrgProjectId);
                    m.MapField("updated", i => i.Updated);
                    m.MapField("state", i => i.State.State);
                    m.MapField("state.isComplete", i => i.State.IsCompleted);
                    m.MapField("provisioningStatus.state", i => i.ProvisioningStatus.State);
                    m.MapField("proposedPerson.azureUniqueId", x => x.ProposedPerson.AzureUniqueId);
                });
            }

            var totalCount = await query.CountAsync(cancellationToken);
            if (totalCount == 0)
                totalCount = 100;

            var skip = request.Query.Skip.GetValueOrDefault(0);
            var take = request.Query.Top.GetValueOrDefault(totalCount);

            var pagedQuery = query
                .Select(x => new QueryResourceAllocationRequest(x, null))
                .Skip(skip)
                .Take(take)
                .AsAsyncEnumerable();

            log.LogTrace($"Analytics query executed with total count: {totalCount}");

            return new PagedAsyncResult<QueryResourceAllocationRequest>(totalCount, pagedQuery);
        }
    }
}