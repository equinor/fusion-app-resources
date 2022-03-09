using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands.Departments
{
    public class SetDepartmentAutoApproval : IRequest
    {
        public string DepartmentId { get; }
        public bool? Enabled { get; }
        public bool? IncludeSubDepartments { get; }

        private bool ShouldRemoveStatus => Enabled is null;

        private SetDepartmentAutoApproval(string departmentId, bool enabled, bool includeSubDepartments)
        {
            DepartmentId = departmentId;
            Enabled = enabled;
            IncludeSubDepartments = includeSubDepartments;
        }
        /// <summary>
        /// Remove status for department.
        /// </summary>
        /// <param name="departmentId">Full department path</param>
        private SetDepartmentAutoApproval(string departmentId)
        {
            DepartmentId = departmentId;
        }

        /// <summary>
        /// Update the status for the department.
        /// </summary>
        /// <param name="departmentId">Full department path</param>
        /// <param name="enabled">Is auto approval enabled or disabled</param>
        /// <param name="includeSubDepartments">Should children be affected by the status</param>
        /// <returns></returns>
        public static SetDepartmentAutoApproval Update(string departmentId, bool enabled, bool includeSubDepartments) 
            => new SetDepartmentAutoApproval(departmentId, enabled, includeSubDepartments);
        /// <summary>
        /// Remove status for the department, will inherit status from parent if any.
        /// </summary>
        /// <param name="departmentId">Full department path</param>
        /// <returns></returns>
        public static SetDepartmentAutoApproval Remove(string departmentId)
            => new SetDepartmentAutoApproval(departmentId);

        public class Handler : AsyncRequestHandler<SetDepartmentAutoApproval>
        {
            private readonly ResourcesDbContext dbContext;

            public Handler(ResourcesDbContext dbContext)
            {
                this.dbContext = dbContext;
            }

            protected override async Task Handle(SetDepartmentAutoApproval request, CancellationToken cancellationToken)
            {
                var currentAllocation = await dbContext.DepartmentAutoApprovals
                    .FirstOrDefaultAsync(d => d.DepartmentFullPath == request.DepartmentId);

                if (request.ShouldRemoveStatus)
                {
                    if (currentAllocation is not null)
                        dbContext.DepartmentAutoApprovals.Remove(currentAllocation);
                }
                else
                {
                    if (currentAllocation is null)
                    {
                        currentAllocation = new DbDepartmentAutoApproval() { DepartmentFullPath = request.DepartmentId };
                        dbContext.DepartmentAutoApprovals.Add(currentAllocation);
                    }

                    currentAllocation.Enabled = request.Enabled.GetValueOrDefault(false);
                    currentAllocation.IncludeSubDepartments = request.IncludeSubDepartments.GetValueOrDefault(true);
                }

                await dbContext.SaveChangesAsync();
            }
        }
    }
}
