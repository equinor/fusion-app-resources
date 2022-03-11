using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    /// <summary>
    /// Checks the auto approval status for the specific department.
    /// The status will be calculated by looking at parents where children/sub departments are included.
    /// 
    /// If no relevant status is located for specific or any parent department, null is returned.
    /// </summary>
    public class GetDepartmentAutoApprovalStatus : IRequest<QueryDepartmentAutoApprovalStatus?>
    {
        public string DepartmentId { get; }

        public GetDepartmentAutoApprovalStatus(string departmentId)
        {
            DepartmentId = departmentId;
        }

        public class Handler : IRequestHandler<GetDepartmentAutoApprovalStatus, QueryDepartmentAutoApprovalStatus?>
        {
            private readonly ResourcesDbContext dbContext;

            public Handler(ResourcesDbContext dbContext)
            {
                this.dbContext = dbContext;
            }

            public async Task<QueryDepartmentAutoApprovalStatus?> Handle(GetDepartmentAutoApprovalStatus request, CancellationToken cancellationToken)
            {
                var path = new DepartmentPath(request.DepartmentId);
                var allRelevantDepartments = path.GetAllParents().Union(new[] { request.DepartmentId });

                var dbItems = await dbContext.DepartmentAutoApprovals.Where(d => allRelevantDepartments.Contains(d.DepartmentFullPath))
                    .AsNoTracking()
                    .ToListAsync();

                // Check if there is a direct assignment at the current department, then use that. 
                var directAssignment = dbItems.FirstOrDefault(i => string.Equals(i.DepartmentFullPath, request.DepartmentId, StringComparison.OrdinalIgnoreCase));
                if (directAssignment is not null)
                {
                    return new QueryDepartmentAutoApprovalStatus(directAssignment, false);
                }


                // Fetch the nearest parent that has children included.
                var nearestEffectiveItem = dbItems.Where(i => i.IncludeSubDepartments).OrderBy(i => i.DepartmentFullPath).LastOrDefault();
                if (nearestEffectiveItem is not null)
                {
                    return new QueryDepartmentAutoApprovalStatus(nearestEffectiveItem, true);
                }

                return null;
            }
        }
    }
}
