using Fusion.Integration.Diagnostics;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.Internal;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Notifications
{
    public partial class ProfileUpdated
    {
        public class ProfileUpdatedHandler : INotificationHandler<ProfileUpdated>
        {
            private readonly ResourcesDbContext dbContext;
            private readonly IFusionLogger<ProfileUpdatedHandler> logger;
            private readonly IProfileCache fusionProfileResolverCache;

            public ProfileUpdatedHandler(ResourcesDbContext dbContext, IFusionLogger<ProfileUpdatedHandler> logger, IProfileCache fusionProfileResolverCache)
            {
                this.dbContext = dbContext;
                this.logger = logger;
                this.fusionProfileResolverCache = fusionProfileResolverCache;
            }

            public async Task Handle(ProfileUpdated notification, CancellationToken cancellationToken)
            {
                var profile = await ResolveDbPersonAsync(notification.Profile);

                if (profile is null)
                {
                    return;
                }

                var hasChanged = profile.JobTitle != notification.Profile.JobTitle
                    || profile.Mail != (notification.Profile.Mail ?? notification.Profile.PreferredContactMail)
                    || profile.Name != notification.Profile.Name
                    || profile.Phone != notification.Profile.MobilePhone
                    || profile.AccountType != $"{notification.Profile.AccountType}";

                if (hasChanged)
                {
                    profile.JobTitle = notification.Profile.JobTitle;
                    profile.Mail = notification.Profile.Mail ?? notification.Profile.PreferredContactMail;
                    profile.Name = notification.Profile.Name;
                    profile.Phone = notification.Profile.MobilePhone;
                    profile.AccountType = $"{notification.Profile.AccountType}";

                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Updated profile for {UPN} ({UniqueId})", notification.Profile.UPN, notification.Profile.AzureUniqueId);

                }

                // Received profile updated event. Remove from profile resolver cache to get a fresh profile in next request.
                await fusionProfileResolverCache.RemoveAsync(notification.Profile.Identifier);
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
}
