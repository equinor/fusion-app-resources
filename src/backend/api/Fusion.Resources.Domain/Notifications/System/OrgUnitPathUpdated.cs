using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Notifications.System
{
    public class OrgUnitPathUpdated : INotification
    {
        public OrgUnitPathUpdated(string sapId, string fullDepartment, string newFullDepartment)
        {
            SapId = sapId;
            FullDepartment = fullDepartment;
            NewFullDepartment = newFullDepartment;
        }

        public string SapId { get; }
        public string FullDepartment { get; }
        public string NewFullDepartment { get; }


        public class RequestAssignedDepartmentHandler : INotificationHandler<OrgUnitPathUpdated>
        {
            private readonly ILogger<RequestAssignedDepartmentHandler> logger;
            private readonly ResourcesDbContext db;

            public RequestAssignedDepartmentHandler(ILogger<RequestAssignedDepartmentHandler> logger, ResourcesDbContext db)
            {
                this.logger = logger;
                this.db = db;
            }

            public async Task Handle(OrgUnitPathUpdated notification, CancellationToken cancellationToken)
            {

                var affectedReqeusts = await db.ResourceAllocationRequests.Where(r => r.AssignedDepartmentId == notification.SapId || (r.AssignedDepartmentId == null && r.AssignedDepartment == notification.FullDepartment))                    
                    .ToListAsync();

                // TODO: Could perhaps generate a summary notification here, informing requests are assigned.
                
                // As this is only a lookup id, we can just update the properties behind the scene instead of doing it through a request.
                foreach (var affected in affectedReqeusts)
                {
                    logger.LogInformation("Updating assigned department for request {RequestNumber} to {SapId} [{FullDepartment}]", affected.RequestNumber, notification.SapId, notification.NewFullDepartment);

                    affected.AssignedDepartment = notification.NewFullDepartment;
                    affected.AssignedDepartmentId = notification.SapId;
                }

                await db.SaveChangesAsync();

            }
        }
    }
}
