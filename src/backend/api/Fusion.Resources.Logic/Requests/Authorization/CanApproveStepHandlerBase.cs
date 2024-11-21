using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Integration.LineOrg;
using Fusion.Resources.Api.Authorization.Requirements;
using Fusion.Resources.Authorization.Requirements;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Requests
{
    public class CanApproveStepHandlerBase
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public CanApproveStepHandlerBase(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        protected async Task CheckAccess(DbResourceAllocationRequest request, WorkflowAccess row)
        {
            var httpRequest = httpContextAccessor?.HttpContext?.Request;
            if (httpRequest is null) throw new UnauthorizedWorkflowException("Http request context could not be found.");

            var result = await httpRequest.RequireAuthorizationAsync(builder =>
            {

                builder.AlwaysAccessWhen(or =>
                {
                    or.BeTrustedApplication();
                    or.GlobalRoleAccess("Fusion.Resources.FullControl");
                    or.GlobalRoleAccess("Fusion.Resources.Internal.FullControl");
                });

                builder.AnyOf(or =>
                {
                    if (!string.IsNullOrEmpty(request.AssignedDepartment))
                    {
                        var path = new DepartmentPath(request.AssignedDepartment);

                        if (row.IsAllResourceOwnersAllowed)
                            or.BeResourceOwnerForDepartment(path.GoToLevel(2), includeDescendants: true);

                        if (row.IsParentResourceOwnerAllowed)
                            or.BeResourceOwnerForDepartment(path.Parent(), includeDescendants: false);

                        if (row.IsSiblingResourceOwnerAllowed)
                            or.BeResourceOwnerForDepartment(path.Parent(), includeDescendants: true);

                        if (row.IsResourceOwnerAllowed)
                        {
                            or.BeResourceOwnerForDepartment(request.AssignedDepartment, includeDescendants: false);
                            or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(request.AssignedDepartment), AccessRoles.ResourceOwner);
                        }
                    }

                    if (row.IsCreatorAllowed)
                    {
                        or.AddRule(new RequestCreatorRequirement(request.Id));
                    }

                    if (row.IsOrgChartTaskOwnerAllowed)
                    {
                        or.BeTaskOwnerInProject(request.Project.OrgProjectId);
                    }

                    if (request.OrgPositionId.HasValue)
                    {
                        if (row.IsOrgChartWriteAllowed)
                        {
                            or.AddRule(OrgPositionAccessRequirement.OrgPositionWrite(request.Project.OrgProjectId, request.OrgPositionId.Value));
                        }
                        if (row.IsOrgChartReadAllowed)
                        {
                            or.AddRule(OrgPositionAccessRequirement.OrgPositionRead(request.Project.OrgProjectId, request.OrgPositionId.Value));
                        }
                        if (row.IsDirectTaskOwnerAllowed)
                        {
                            var requirement = new TaskOwnerForPositionRequirement(
                                request.Project.OrgProjectId,
                                request.OrgPositionId.Value,
                                request.OrgPositionInstance.Id
                            );
                            or.AddRule(requirement);
                        }
                    }

                    if (row.IsOrgAdminAllowed)
                    {
                        or.AddRule(OrgProjectAccessRequirement.OrgWrite(request.Project.OrgProjectId));
                    }
                });
            });

            if (!result.Success) throw ThrowFromAuthOutcome(result);
        }

        private static UnauthorizedWorkflowException ThrowFromAuthOutcome(AuthorizationOutcome result)
        {
            var failedRequirements = result.Rules.Where(r => r.Outcome?.Succeeded == false)
                .SelectMany(r => r.Outcome?.Failure!.FailedRequirements ?? Array.Empty<IAuthorizationRequirement>())
                .OfType<IReportableAuthorizationRequirement>()
                .Distinct()
                .ToArray();

            var err = new StringBuilder();
            err.AppendLine("None of the requirements for this operation succeeded.");
            foreach(var req in failedRequirements)
            {
                err.AppendFormat("\t- {0}\n", req.Description);
            }

            return new UnauthorizedWorkflowException(err.ToString(), result.Exception)
            {
                Requirements = failedRequirements
            };
        }
    }
}
