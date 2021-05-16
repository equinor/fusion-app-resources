using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        class CanApproveResourceOwnerStepHandler : INotificationHandler<CanApproveStep>
        {
            private static Dictionary<string, WorkflowAccess> AccessTable = new Dictionary<string, WorkflowAccess>
            {
                [ResourceOwnerChangeWorkflowV1.CREATED] = WorkflowAccess.Default with 
                {
                    IsResourceOwnerAllowed = true,
                    IsParentResourceOwnerAllowed = true,
                    IsSiblingResourceOwnerAllowed = true,
                    IsCreatorAllowed = true,
                },
                [ResourceOwnerChangeWorkflowV1.ACCEPTANCE] = WorkflowAccess.Default with
                {
                    IsDirectTaskOwnerAllowed = true,
                    IsOrgChartWriteAllowed = true,
                    IsOrgAdminAllowed = true
                },
                [WorkflowDefinition.PROVISIONING] = WorkflowAccess.Default,
            };
            private readonly ResourcesDbContext dbContext;
            private readonly IHttpContextAccessor httpContextAccessor;

            public CanApproveResourceOwnerStepHandler(ResourcesDbContext dbContext, IHttpContextAccessor httpContextAccessor)
            {
                this.dbContext = dbContext;
                this.httpContextAccessor = httpContextAccessor;
            }

            public async Task Handle(CanApproveStep notification, CancellationToken cancellationToken)
            {
                if (notification.Type != DbInternalRequestType.ResourceOwnerChange) return;

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
}
