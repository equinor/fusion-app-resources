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
    /// <summary>
    /// List all entries for auto approvals.
    /// 
    /// Allows filtering on 
    /// - FullDepartmentPath
    /// - Enabled
    /// - IncludeSubDepartments
    /// </summary>
    public class ListDepartmentAutoApprovals : IRequest<IEnumerable<QueryDepartmentAutoApprovalStatus>>
    {

        public ODataQueryParams Query { get; private set; } = new ODataQueryParams();

        public ListDepartmentAutoApprovals WithQuery(ODataQueryParams query)
        {
            Query = query;
            return this;
        }

        public class Handler : IRequestHandler<ListDepartmentAutoApprovals, IEnumerable<QueryDepartmentAutoApprovalStatus>>
        {
            private readonly ResourcesDbContext dbContext;

            public Handler(ResourcesDbContext dbContext)
            {
                this.dbContext = dbContext;
            }

            public async Task<IEnumerable<QueryDepartmentAutoApprovalStatus>> Handle(ListDepartmentAutoApprovals request, CancellationToken cancellationToken)
            {
                var itemsQuery = dbContext.DepartmentAutoApprovals.AsQueryable();

                itemsQuery = itemsQuery.ApplyODataFilters(request.Query, m =>
                {
                    m.MapField(nameof(QueryDepartmentAutoApprovalStatus.FullDepartmentPath), e => e.DepartmentFullPath);
                    m.MapField(nameof(QueryDepartmentAutoApprovalStatus.Enabled), e => e.Enabled);
                    m.MapField(nameof(QueryDepartmentAutoApprovalStatus.IncludeSubDepartments), e => e.IncludeSubDepartments);
                });


                var items = await itemsQuery
                    .OrderByDescending(i => i.DepartmentFullPath)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken: cancellationToken);

                return items.Select(i => new QueryDepartmentAutoApprovalStatus(i, false))
                    .ToList();
            }
        }
    }
}
