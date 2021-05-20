using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
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
        protected async Task EvaluateAccess(DbResourceAllocationRequest request, WorkflowAccess row, ClaimsPrincipal initiator)
        {
            bool isAllowed = false;

            if (!string.IsNullOrEmpty(request.AssignedDepartment))
            {
                var path = new DepartmentPath(request.AssignedDepartment);

                if (row.IsAllResourceOwnersAllowed)
                    isAllowed |= initiator.IsResourceOwner(path.GoToLevel(2), includeChildDepartments: true);
                if (row.IsParentResourceOwnerAllowed)
                    isAllowed |= initiator.IsResourceOwner(path.Parent(), includeChildDepartments: false); ;
                if (row.IsSiblingResourceOwnerAllowed)
                    isAllowed |= initiator.IsResourceOwner(path.Parent(), includeChildDepartments: true);

                if (row.IsResourceOwnerAllowed)
                    isAllowed |= initiator.IsResourceOwner(request.AssignedDepartment, includeChildDepartments: true);
            }

            if (row.IsCreatorAllowed)
                isAllowed |= initiator.GetAzureUniqueIdOrThrow() == request.CreatedBy.AzureUniqueId;

            if (row.IsOrgChartTaskOwnerAllowed)
                isAllowed |= initiator.IsTaskOwnerInProject(request.Project.OrgProjectId);

            isAllowed |= initiator.IsApplicationUser();
            isAllowed |= initiator.IsInRole("Fusion.Resources.FullControl");
            isAllowed |= initiator.IsInRole("Fusion.Resources.Internal.FullControl");

            if (!isAllowed) throw new UnauthorizedWorkflowException();
        }
    }
}
