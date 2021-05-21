using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Logic.Requests;
using Fusion.Resources.Logic.Workflows;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
        class CanApproveResourceOwnerStepHandler : CanApproveStepHandlerBase, INotificationHandler<CanApproveStep>
        {
            private static readonly Dictionary<string, WorkflowAccess> AccessTable = new Dictionary<string, WorkflowAccess>
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

            public CanApproveResourceOwnerStepHandler(
                ResourcesDbContext dbContext,
                IAuthorizationService authService,
                IHttpContextAccessor httpContextAccessor) : base(authService)
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

                var row = AccessTable[notification.NextStepId!];

                await CheckAccess(request, row, initiator);
            }
        }
    }
}
