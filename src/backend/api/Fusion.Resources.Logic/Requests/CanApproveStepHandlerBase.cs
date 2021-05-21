using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Resources.Api.Authorization.Requirements;
using Fusion.Resources.Authorization.Requirements;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Requests
{
    public record WorkflowAccessKey(string Subtype, string CurrentStep)
    {
        public static implicit operator WorkflowAccessKey((string, string) tuple)
            => new WorkflowAccessKey(tuple.Item1, tuple.Item2);
    }
    public record WorkflowAccess()
    {
        public bool IsResourceOwnerAllowed { get; init; } = false;
        public bool IsParentResourceOwnerAllowed { get; init; } = false;
        public bool IsSiblingResourceOwnerAllowed { get; init; } = false;
        public bool IsAllResourceOwnersAllowed { get; init; } = false;
        public bool IsCreatorAllowed { get; init; } = false;
        public bool IsDirectTaskOwnerAllowed { get; init; } = false;
        public bool IsOrgChartTaskOwnerAllowed { get; init; } = false;
        public bool IsOtherProjectMembersAllowed { get; init; } = false;
        public bool IsOrgChartReadAllowed { get; init; } = false;
        public bool IsOrgChartWriteAllowed { get; init; } = false;
        public bool IsOrgAdminAllowed { get; init; } = false;

        public static WorkflowAccess Default = new WorkflowAccess();
    }

    public class CanApproveStepHandlerBase
    {
        private readonly IAuthorizationService authorizationService;

        public CanApproveStepHandlerBase(IAuthorizationService authorizationService)
        {
            this.authorizationService = authorizationService;
        }
        protected async Task CheckAccess(DbResourceAllocationRequest request, WorkflowAccess row, ClaimsPrincipal initiator)
        {
            bool isAllowed = false;

            if (!string.IsNullOrEmpty(request.AssignedDepartment))
            {
                var path = new DepartmentPath(request.AssignedDepartment);

                if (row.IsAllResourceOwnersAllowed)
                {
                    var result = await authorizationService.AuthorizeAsync(initiator, request, new ResourceOwnerRequirement(path.GoToLevel(2), includeDescendants: true));
                    isAllowed |= result.Succeeded;
                }
                if (row.IsParentResourceOwnerAllowed)
                {
                    var result = await authorizationService.AuthorizeAsync(initiator, request, new ResourceOwnerRequirement(path.Parent(), includeDescendants: false));
                    isAllowed |= result.Succeeded;
                }
                if (row.IsSiblingResourceOwnerAllowed)
                {
                    var result = await authorizationService.AuthorizeAsync(initiator, request, new ResourceOwnerRequirement(path.Parent(), includeDescendants: true));
                    isAllowed |= result.Succeeded;
                }

                if (row.IsResourceOwnerAllowed)
                {
                    var result = await authorizationService.AuthorizeAsync(initiator, request, new ResourceOwnerRequirement(request.AssignedDepartment, includeDescendants: false));
                    isAllowed |= result.Succeeded;
                }
            }

            if (row.IsCreatorAllowed)
            {
                var result = await authorizationService.AuthorizeAsync(initiator, request, new RequestCreatorRequirement(request.Id));
                isAllowed |= result.Succeeded;
            }

            if (row.IsOrgChartTaskOwnerAllowed)
            {
                var result = await authorizationService.AuthorizeAsync(initiator, request.Project.OrgProjectId, new TaskOwnerInProjectRequirement());
                isAllowed |= result.Succeeded;
            }

            if(request.OrgPositionId.HasValue)
            {
                if (row.IsOrgChartWriteAllowed)
                {
                    var result = await authorizationService.AuthorizeAsync(initiator, request, OrgPositionAccessRequirement.OrgPositionWrite(request.Project.OrgProjectId, request.OrgPositionId.Value));
                    isAllowed |= result.Succeeded;
                }
                if(row.IsOrgChartReadAllowed)
                {
                    var result = await authorizationService.AuthorizeAsync(initiator, request, OrgPositionAccessRequirement.OrgPositionRead(request.Project.OrgProjectId, request.OrgPositionId.Value));
                    isAllowed |= result.Succeeded;
                }
                if (row.IsDirectTaskOwnerAllowed)
                {
                    var requirement = new TaskOwnerForPositionRequirement(
                        request.Project.OrgProjectId, 
                        request.OrgPositionId.Value,
                        request.OrgPositionInstance.Id
                    );
                    var result = await authorizationService.AuthorizeAsync(initiator, request.Project.OrgProjectId, requirement);
                    isAllowed |= result.Succeeded;
                }
            }
            
            if(row.IsOrgAdminAllowed)
            {
                var result = await authorizationService.AuthorizeAsync(initiator, request, OrgProjectAccessRequirement.OrgWrite(request.Project.OrgProjectId));
                isAllowed |= result.Succeeded;
            }

            isAllowed |= initiator.IsApplicationUser();
            isAllowed |= initiator.IsInRole("Fusion.Resources.FullControl");
            isAllowed |= initiator.IsInRole("Fusion.Resources.Internal.FullControl");

            if (!isAllowed) throw new UnauthorizedWorkflowException();
        }
    }
}
