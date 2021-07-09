using Fusion.Integration.Diagnostics;
using Fusion.Integration.Profile;
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
        public class PersonnelUpdatedHandler : INotificationHandler<ProfileUpdated>
        {
            private readonly ResourcesDbContext dbContext;
            private readonly IFusionLogger<PersonnelUpdatedHandler> logger;

            public PersonnelUpdatedHandler(ResourcesDbContext dbContext, IFusionLogger<PersonnelUpdatedHandler> logger)
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


                // We only want to sync the preferred contact mail 
                var hasChanged = profile.PreferredContractMail != notification.Profile.PreferredContactMail;

                if (hasChanged)
                {
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Updated personnel profile for {UPN} ({UniqueId})", notification.Profile.UPN, notification.Profile.AzureUniqueId);
                }
            }

            private async Task<DbExternalPersonnelPerson?> ResolveDbPersonAsync(FusionPersonProfile profile)
            {
                if (profile.AzureUniqueId.HasValue)
                    return await dbContext.ExternalPersonnel.FirstOrDefaultAsync(p => p.AzureUniqueId == profile.AzureUniqueId.Value);

                // Must avoid null == null compares..
                if (!string.IsNullOrEmpty(profile.Mail))
                    return await dbContext.ExternalPersonnel.FirstOrDefaultAsync(p => p.Mail == profile.Mail);

                // No profile matches...
                return null;
            }
        }
    }
}
