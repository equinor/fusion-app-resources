using Fusion.Integration.Diagnostics;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Notifications.Handlers
{
    public class ClearPersonNotesHandler : INotificationHandler<ProfileRemovedFromCompany>
    {
        private ResourcesDbContext dbContext;
        private IFusionLogger<ClearPersonNotesHandler> logger;

        public ClearPersonNotesHandler(ResourcesDbContext dbContext, IFusionLogger<ClearPersonNotesHandler> logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }

        public async Task Handle(ProfileRemovedFromCompany notification, CancellationToken cancellationToken)
        {
            var notes = await dbContext.PersonNotes.Where(n => n.AzureUniqueId == notification.AzureUniqueId)
                .ToListAsync();
            dbContext.RemoveRange(notes);

            if (notes.Any())
                logger.LogInformation("Removing {NoteCount} notes for user {UniqueId}", notes.Count, notification.AzureUniqueId);

            await dbContext.SaveChangesAsync();
        }
    }
}
