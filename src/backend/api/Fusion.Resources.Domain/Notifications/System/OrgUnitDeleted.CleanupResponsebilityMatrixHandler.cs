using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Notifications.System
{
    public partial class OrgUnitDeleted
    {
        /// <summary>
        /// Remove all items in the responsebility matrix that route requests to the deleted department.
        /// </summary>
        public class CleanupResponsebilityMatrixHandler : INotificationHandler<OrgUnitDeleted>
        {
            private readonly ILogger<DeleteDraftChangeRequestsHandler> logger;
            private readonly ResourcesDbContext db;

            public CleanupResponsebilityMatrixHandler(ILogger<DeleteDraftChangeRequestsHandler> logger, ResourcesDbContext db)
            {
                this.logger = logger;
                this.db = db;
            }

            public async Task Handle(OrgUnitDeleted notification, CancellationToken cancellationToken)
            {
                // No good query to get specific items, it is either all or nothing. Fetching directly from the db.
                var relevantItems = await db.ResponsibilityMatrices.Where(i => i.Unit == notification.FullDepartment).ToListAsync();

                if (relevantItems.Any())
                {
                    logger.LogInformation($"Deleting {relevantItems.Count} responsebility matrix items for {notification.FullDepartment}");

                    db.ResponsibilityMatrices.RemoveRange(relevantItems);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
