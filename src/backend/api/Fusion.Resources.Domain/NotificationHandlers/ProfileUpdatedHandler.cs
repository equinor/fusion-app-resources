using Fusion.Integration.Diagnostics;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Notifications.Handlers
{
    public class ProfileUpdatedHandler : INotificationHandler<ProfileUpdated>
    {
        private readonly ResourcesDbContext dbContext;
        private readonly IFusionLogger<ProfileUpdatedHandler> logger;

        public ProfileUpdatedHandler(ResourcesDbContext dbContext, IFusionLogger<ProfileUpdatedHandler> logger)
        {
            this.dbContext = dbContext;
            this.logger = logger;
        }

        public async Task Handle(ProfileUpdated notification, CancellationToken cancellationToken)
        {
            var profile = await ResolveDbPersonAsync(notification.Profile);

            if (profile is null)
            {
                return;
            }

            var hasChanged = profile.JobTitle != profile.JobTitle
                || profile.Mail != profile.Mail
                || profile.Name != profile.Name
                || profile.Phone != profile.Phone
                || profile.AccountType != $"{profile.AccountType}";

            profile.JobTitle = profile.JobTitle;
            profile.Mail = profile.Mail;
            profile.Name = profile.Name;
            profile.Phone = profile.Phone;
            profile.AccountType = $"{profile.AccountType}";

            if (hasChanged)
            {
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Updated profile for {UPN} ({UniqueId})", notification.Profile.UPN, notification.Profile.AzureUniqueId);
            }
        }

        private async Task<DbPerson?> ResolveDbPersonAsync(FusionPersonProfile profile)
        {
            if (profile.AzureUniqueId.HasValue)
                return await dbContext.Persons.FirstOrDefaultAsync(p => p.AzureUniqueId == profile.AzureUniqueId.Value);

            // Must avoid null == null compares..
            if (!string.IsNullOrEmpty(profile.Mail))
                return await dbContext.Persons.FirstOrDefaultAsync(p => p.Mail == profile.Mail);

            // No profile matches...
            return null;
        }
    }
}
