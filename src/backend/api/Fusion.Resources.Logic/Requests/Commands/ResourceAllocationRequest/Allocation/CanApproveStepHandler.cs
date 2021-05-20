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

        public class CanApproveStepHandler : INotificationHandler<CanApproveStep>
        {
            private static readonly Dictionary<WorkflowAccessKey, WorkflowAccess> AccessTable = new Dictionary<WorkflowAccessKey, WorkflowAccess>
            {
                [(AllocationNormalWorkflowV1.SUBTYPE, AllocationNormalWorkflowV1.PROPOSAL)] = WorkflowAccess.Default with
                {
                    IsResourceOwnerAllowed = true,
                    IsAllResourceOwnersAllowed = true,
                },
                [(AllocationNormalWorkflowV1.SUBTYPE, AllocationNormalWorkflowV1.APPROVAL)] = WorkflowAccess.Default with
                {
                    IsDirectTaskOwnerAllowed = true,
                    IsOrgAdminAllowed = true,
                    IsOrgChartWriteAllowed = true,
                    IsCreatorAllowed = true
                },
                [(AllocationNormalWorkflowV1.SUBTYPE, WorkflowDefinition.PROVISIONING)] = WorkflowAccess.Default,

                [(AllocationJointVentureWorkflowV1.SUBTYPE, AllocationJointVentureWorkflowV1.APPROVAL)]= WorkflowAccess.Default with
                {
                    IsResourceOwnerAllowed = true,
                    IsParentResourceOwnerAllowed = true,
                    IsSiblingResourceOwnerAllowed = true,
                    IsCreatorAllowed = true,
                },
                [(AllocationJointVentureWorkflowV1.SUBTYPE, WorkflowDefinition.PROVISIONING)]
                    = WorkflowAccess.Default,

                [(AllocationEnterpriseWorkflowV1.SUBTYPE, WorkflowDefinition.PROVISIONING)]
                    = WorkflowAccess.Default,

                [(AllocationDirectWorkflowV1.SUBTYPE, WorkflowDefinition.PROVISIONING)]
                    = WorkflowAccess.Default,
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
                var row = AccessTable[(request.SubType!.ToLower(), notification.NextStepId!)];

                bool isAllowed = false;

                if (!string.IsNullOrEmpty(request.AssignedDepartment))
                {
                    var path = new DepartmentPath(request.AssignedDepartment);

                    if (row.IsAllResourceOwnersAllowed)
                        isAllowed |= initiator.IsResourceOwner(path.GoToLevel(2), includeChildDepartments: true);
                    if (row.IsParentResourceOwnerAllowed)
                        isAllowed |= initiator.IsResourceOwner(path.Parent(), includeChildDepartments: false);;
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
}
