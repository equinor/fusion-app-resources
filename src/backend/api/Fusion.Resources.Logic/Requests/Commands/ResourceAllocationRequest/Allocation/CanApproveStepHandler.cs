using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Logic.Requests;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public class CanApproveStepHandler : CanApproveStepHandlerBase, INotificationHandler<CanApproveStep>
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

                var row = AccessTable[(request.SubType!.ToLower(), notification.NextStepId!)];

                await EvaluateAccess(request, row, initiator);
            }
        }
    }
}
