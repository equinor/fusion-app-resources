using Fusion.Resources.Database;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fusion.Resources.Domain.Queries
{
    public class GetResourceAllocationRequestsForAnalytics : IRequest<QueryRangedList<QueryResourceAllocationRequest>>
    {
        public GetResourceAllocationRequestsForAnalytics(ODataQueryParams query)
        {
            this.Query = query;
        }
        public ODataQueryParams Query { get; }


        public class Handler : IRequestHandler<GetResourceAllocationRequestsForAnalytics, QueryRangedList<QueryResourceAllocationRequest>>
        {
            private readonly ResourcesDbContext db;
            private readonly IFusionLogger<GetResourceAllocationRequestsForAnalytics> log;

            public Handler(ResourcesDbContext db, IFusionLogger<GetResourceAllocationRequestsForAnalytics> log)
            {
                this.db = db;
                this.log = log;
            }

            public async Task<QueryRangedList<QueryResourceAllocationRequest>> Handle(GetResourceAllocationRequestsForAnalytics request, CancellationToken cancellationToken)
            {

                var query = db.ResourceAllocationRequests
                    .Include(r => r.OrgPositionInstance)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.ProposedPerson)
                    .OrderBy(x => x.Id) // Should have consistent sorting due to OData criterion.
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
                var skip = request.Query.Skip.GetValueOrDefault(0);
                var take = request.Query.Top.GetValueOrDefault(totalCount);

                var pagedQuery = await QueryRangedList.FromQueryAsync(query.Select(x => new QueryResourceAllocationRequest(x, null)), skip, take);

                log.LogTrace($"Analytics query executed");

                return pagedQuery;
            }
        }
    }
}