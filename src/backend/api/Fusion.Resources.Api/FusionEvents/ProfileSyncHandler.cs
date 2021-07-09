using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.FusionEvents
{
    public class ProfileSyncHandler : IProfileSyncHandler
    {
        private readonly IMediator mediator;
        private readonly ResourcesDbContext dbContext;

        public ProfileSyncHandler(IMediator mediator, ResourcesDbContext dbContext)
        {
            this.mediator = mediator;
            this.dbContext = dbContext;
        }

        public async Task ProcessProfileAsync(IProfileSyncHandler.ProfileUpdatedEventContext context)
        {
            // Check if we are tracking this profile.
            var isTracked = await dbContext.Persons.AnyAsync(p => p.AzureUniqueId == context.Person.AzureUniquePersonId || (p.Mail != null && p.Mail == context.Person.Mail));
            var isPersonell = await dbContext.ExternalPersonnel.AnyAsync(p => p.AzureUniqueId == context.Person.AzureUniquePersonId || (p.Mail != null && p.Mail == context.Person.Mail));

            if (isTracked || isPersonell)
            {
                var updatedProfile = await context.ResolveProfile.Value;
                if  (updatedProfile is not null) await mediator.Publish(new Domain.Notifications.ProfileUpdated(updatedProfile));
            }

            if (context.WasRemoved)
            {
                await mediator.Publish(new Domain.Notifications.ProfileRemovedFromCompany(context.Person.AzureUniquePersonId));
            }
        }
    }
}
