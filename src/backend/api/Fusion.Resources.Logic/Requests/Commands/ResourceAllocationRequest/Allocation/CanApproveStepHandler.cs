using Fusion.Resources.Database.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using Fusion.Resources.Logic.Workflows;
using System.Collections.Generic;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public record WorkflowAccessKey(string Subtype, string CurrentStep)
        {
            public static implicit operator WorkflowAccessKey((string, string) tuple)
                => new WorkflowAccessKey(tuple.Item1, tuple.Item2);
        }
        public record WorkflowAccess(
            bool IsResourceOwnerAllowed,
            bool IsAllResourceOwnersAllowed,
            bool IsCreatorAllowed,
            bool IsDirectTaskOwnerAllowed,
            bool IsOrgChartTaskOwnerAllowed,
            bool IsOtherProjectMembersAllowed,
            bool IsOrgChartReadAllowed,
            bool IsOrgChartWriteAllowed,
            bool IsOrgAdminAllowed
        );

        public class CanApproveStepHandler : INotificationHandler<CanApproveStep>
        {
            private static Dictionary<WorkflowAccessKey, WorkflowAccess> AccessTable = new Dictionary<WorkflowAccessKey, WorkflowAccess>
            {
                [(AllocationNormalWorkflowV1.SUBTYPE, AllocationNormalWorkflowV1.PROPOSAL)]
                    = new WorkflowAccess(true, true, false, false, false, false, false, false, false),
                [(AllocationNormalWorkflowV1.SUBTYPE, AllocationNormalWorkflowV1.APPROVAL)]
                    = new WorkflowAccess(false, false, true, false, false, false, false, true, true),
                [(AllocationNormalWorkflowV1.SUBTYPE, AllocationNormalWorkflowV1.PROVISIONING)]
                    = new WorkflowAccess(false, false, false, false, false, false, false, false, false),

                [(AllocationJointVentureWorkflowV1.SUBTYPE, AllocationJointVentureWorkflowV1.APPROVAL)]
                    = new WorkflowAccess(true, true, false, true, false, false, false, false, false),
                [(AllocationJointVentureWorkflowV1.SUBTYPE, AllocationJointVentureWorkflowV1.PROVISIONING)]
                    = new WorkflowAccess(false, false, false, false, false, false, false, false, false),

                [(AllocationEnterpriseWorkflowV1.SUBTYPE, AllocationEnterpriseWorkflowV1.PROVISIONING)]
                    = new WorkflowAccess(false, false, false, false, false, false, false, false, false),

                [(AllocationDirectWorkflowV1.SUBTYPE, AllocationDirectWorkflowV1.PROVISIONING)]
                    = new WorkflowAccess(false, false, false, false, false, false, false, false, false),
            };
            private readonly ResourcesDbContext dbContext;
            private readonly IHttpContextAccessor httpContextAccessor;

            public CanApproveStepHandler(ResourcesDbContext dbContext, IHttpContextAccessor httpContextAccessor)
            {
                this.dbContext = dbContext;
                this.httpContextAccessor = httpContextAccessor;
            }

            public async Task Handle(CanApproveStep notification, CancellationToken cancellationToken)
            {
                if (notification.Type != DbInternalRequestType.Allocation) return;

                var request = await dbContext.ResourceAllocationRequests
                    .Include(p => p.Project)
                    .FirstAsync(r => r.Id == notification.RequestId, cancellationToken: cancellationToken);

                var initiator = httpContextAccessor?.HttpContext?.User;
                if (initiator is null) throw new UnauthorizedWorkflowException();

                await EvaluateAccess(request, notification, initiator);
            }

            private async Task EvaluateAccess(DbResourceAllocationRequest request, CanApproveStep notification, System.Security.Claims.ClaimsPrincipal initiator)
            {
                var row = AccessTable[(request.SubType!, notification.NextStepId!)];

                bool isAllowed = false;

                if (!string.IsNullOrEmpty(request.AssignedDepartment))
                {
                    var path = new DepartmentPath(request.AssignedDepartment);

                    if (row.IsAllResourceOwnersAllowed)
                        isAllowed |= initiator.IsResourceOwner(path.GoToLevel(2), includeChildDepartments: true);

                    if (row.IsResourceOwnerAllowed)
                        isAllowed |= initiator.IsResourceOwner(path.Parent(), includeChildDepartments: true);
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
}
