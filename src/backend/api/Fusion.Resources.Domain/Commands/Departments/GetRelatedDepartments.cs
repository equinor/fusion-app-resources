using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetRelatedDepartments : IRequest<QueryRelatedDepartments?>
    {
        public GetRelatedDepartments(string department, bool isDepartmentManager = false)
        {
            Department = department;
            IsDepartmentManager = isDepartmentManager;
        }

        public string Department { get; }
        public bool IsDepartmentManager { get; }

        public class Handler : IRequestHandler<GetRelatedDepartments, QueryRelatedDepartments?>
        {
            private readonly ILogger<Handler> logger;
            private readonly ILineOrgClient lineOrg;

            public Handler(ILogger<Handler> logger, ILineOrgClient lineOrg)
            {
                this.logger = logger;
                this.lineOrg = lineOrg;
            }

            public async Task<QueryRelatedDepartments?> Handle(GetRelatedDepartments request, CancellationToken cancellationToken)
                => await TryGetRelevantDepartmentsAsync(request.Department, request.IsDepartmentManager);


            private async Task<QueryRelatedDepartments?> TryGetRelevantDepartmentsAsync(string? fullDepartment, bool isDepartmentManager)
            {
                if (fullDepartment is null)
                    return null;

                try
                {
                    return await ResolveRelevantDepartmentsAsync(fullDepartment, isDepartmentManager);

                }
                catch (Exception ex)
                {
                    // Log
                    logger.LogCritical(ex, "Could not resolve relevant departments for {Department}", fullDepartment);
                    return null;
                }
            }

            private async Task<QueryRelatedDepartments?> ResolveRelevantDepartmentsAsync(string fullDepartmentPath, bool isDepartmentManager)
            {
                var relevantDepartments = new QueryRelatedDepartments();


                var orgUnit = await lineOrg.ResolveOrgUnitAsync(fullDepartmentPath, a => a.ExpandChildren());

                if (orgUnit is null)
                    return null;

                // Add children

                // Children should be returned
                if (relevantDepartments.Children is null)
                    throw new NullReferenceException($"Child org unit should have been expanded and not be null for org unit {orgUnit?.FullDepartment}");

                relevantDepartments.Children = orgUnit.Children!.Select(o => new QueryDepartment(o)).ToList();


                // Add siblings

                if (orgUnit.Parent is not null && !isDepartmentManager)
                {
                    var parentOrg = await lineOrg.ResolveOrgUnitAsync(orgUnit.Parent.SapId, a => a.ExpandChildren());

                    if (parentOrg is null) throw new InvalidOperationException($"Parent org unit of {orgUnit.FullDepartment}, using sap id '{orgUnit.Parent.SapId}', does not seem to exist, got null.");

                    if (parentOrg.Children is null)
                        throw new NullReferenceException($"Child org unit should have been expanded and not be null for org unit {orgUnit?.FullDepartment}");

                    relevantDepartments.Siblings = parentOrg.Children!
                        .Where(o => o.SapId != orgUnit.SapId) // Exclude self
                        .Select(o => new QueryDepartment(o))
                        .ToList();

                }

                return relevantDepartments;
            }


        }
    }
}
