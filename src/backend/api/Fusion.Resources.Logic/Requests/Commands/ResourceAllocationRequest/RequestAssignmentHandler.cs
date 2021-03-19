using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public class RequestAssignmentHandler : INotificationHandler<RequestInitialized>
        {
            private readonly ResourcesDbContext db;

            public RequestAssignmentHandler(ResourcesDbContext db)
            {
                this.db = db;
            }
            public async Task Handle(RequestInitialized notification, CancellationToken cancellationToken)
            {
                if (notification.Type != DbInternalRequestType.Normal && notification.Type != DbInternalRequestType.JointVenture) return;

                var request = await db.ResourceAllocationRequests
                    .SingleAsync(r => r.Id == notification.RequestId, cancellationToken);

                if (!string.IsNullOrEmpty(request.AssignedDepartment)) return;

                var router = new RequestRouter(db);

                var department = await router.Route(request, cancellationToken);

                if(department != null)
                {
                    request.AssignedDepartment = department;
                }

                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
