using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace Fusion.Resources.Domain.Services
{
    internal class ProfileServices : IProfileService
    {
        private readonly IFusionProfileResolver profileResolver;
        private readonly ResourcesDbContext resourcesDb;
        private static readonly SemaphoreSlim locker = new SemaphoreSlim(1);

        public ProfileServices(IFusionProfileResolver profileResolver, ResourcesDbContext resourcesDb)
        {
            this.profileResolver = profileResolver;
            this.resourcesDb = resourcesDb;
        }

        public async Task<DbExternalPersonnelPerson?> ResolveExternalPersonnelAsync(PersonId personId)
        {
            var existingEntry = personId.Type switch
            {
                PersonId.IdentifierType.UniqueId => await resourcesDb.ExternalPersonnel.FirstOrDefaultAsync(p => p.AzureUniqueId == personId.UniqueId),
                PersonId.IdentifierType.Mail => await resourcesDb.ExternalPersonnel.FirstOrDefaultAsync(p => p.Mail == personId.Mail),
                _ => throw new InvalidOperationException("Unsupported person identifier type")
            };

            return existingEntry;
        }

        public async Task<DbExternalPersonnelPerson> EnsureExternalPersonnelAsync(PersonId personId)
        {
            await locker.WaitAsync();

            try
            {
                var existingEntry = personId.Type switch
                {
                    PersonId.IdentifierType.Mail => await resourcesDb.ExternalPersonnel.FirstOrDefaultAsync(p => p.Mail == personId.Mail),
                    PersonId.IdentifierType.UniqueId => await resourcesDb.ExternalPersonnel.FirstOrDefaultAsync(p => p.AzureUniqueId == personId.UniqueId),
                    _ => throw new InvalidOperationException("Unsupported person identifier type")
                };

                if (existingEntry != null)
                    return existingEntry;


                var profile = await ResolveProfileAsync(personId);

                var newEntry = new DbExternalPersonnelPerson()
                {
                    AccountStatus = DbAzureAccountStatus.NoAccount,
                    Disciplines = new List<DbPersonnelDiscipline>(),
                    Mail = personId.Mail!,  // Assume mail 
                    Name = personId.Mail!
                };

                if (profile != null)
                {
                    newEntry.Mail = profile.Mail;
                    newEntry.AccountStatus = DbAzureAccountStatus.Available;
                    newEntry.AzureUniqueId = profile.AzureUniqueId;
                    newEntry.JobTitle = profile.JobTitle;
                    newEntry.Name = profile.Name;
                    newEntry.Phone = profile.MobilePhone;
                }

                await resourcesDb.ExternalPersonnel.AddAsync(newEntry);
                await resourcesDb.SaveChangesAsync();
                return newEntry;

            }
            finally
            {
                locker.Release();
            }
        }

        public async Task<DbPerson?> EnsurePersonAsync(Guid azureUniqueId)
        {
            await locker.WaitAsync();

            try
            {
                var person = await resourcesDb.Persons.FirstOrDefaultAsync(p => p.AzureUniqueId == azureUniqueId);
                if (person != null)
                    return person;

                var profile = await ResolveProfileAsync(azureUniqueId);

                if (profile == null)
                    return null;

                person = new DbPerson
                {
                    AccountType = profile.AccountType.ToString(),
                    AzureUniqueId = azureUniqueId,
                    JobTitle = profile.JobTitle,
                    Mail = profile.Mail,
                    Name = profile.Name,
                    Phone = profile.MobilePhone
                };

                await resourcesDb.Persons.AddAsync(person);
                await resourcesDb.SaveChangesAsync();

                return person;
            }
            finally
            {
                locker.Release();
            }
        }

        public async Task<DbPerson?> EnsureApplicationAsync(Guid azureUniqueId)
        {
            await locker.WaitAsync();

            try
            {
                var person = await resourcesDb.Persons.FirstOrDefaultAsync(p => p.AzureUniqueId == azureUniqueId);
                if (person != null)
                    return person;

                var profile = await ResolveApplicationAsync(azureUniqueId);

                if (profile == null)
                    return null;

                person = new DbPerson
                {
                    AccountType = $"{FusionAccountType.Application}",
                    AzureUniqueId = azureUniqueId,
                    JobTitle = "Azure AD Application",
                    Mail = $"{profile.ServicePrincipalId}@{profile.ApplicationId}",
                    Name = profile.DisplayName,
                    Phone = string.Empty
                };

                await resourcesDb.Persons.AddAsync(person);
                await resourcesDb.SaveChangesAsync();

                return person;
            }
            finally
            {
                locker.Release();
            }
        }

        public async Task<FusionPersonProfile?> ResolveProfileAsync(PersonId person)
        {
            try
            {
                return await profileResolver.ResolvePersonBasicProfileAsync(person.OriginalIdentifier);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<FusionApplicationProfile?> ResolveApplicationAsync(Guid servicePrincipalUniqueId)
        {
            try
            {
                return await profileResolver.ResolveServicePrincipalAsync(servicePrincipalUniqueId);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
