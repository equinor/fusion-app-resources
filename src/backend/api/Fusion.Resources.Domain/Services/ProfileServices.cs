using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace Fusion.Resources.Domain.Services
{
    internal class ProfileServices : IProfileService
    {
        private readonly IFusionProfileResolver profileResolver;
        private readonly ResourcesDbContext resourcesDb;
        private static readonly SemaphoreSlim Locker = new SemaphoreSlim(1);

        public ProfileServices(IFusionProfileResolver profileResolver, ResourcesDbContext resourcesDb)
        {
            this.profileResolver = profileResolver;
            this.resourcesDb = resourcesDb;
        }

        public async Task<DbExternalPersonnelPerson?> ResolveExternalPersonnelAsync(PersonId personId)
        {
            // In most cases the same mail could refer to different persons, since the equinor mails are reused, however
            // since this is external ids, we could assume that the mails are not reused.

            var existingEntry = personId.Type switch
            {
                PersonId.IdentifierType.UniqueId => await resourcesDb.ExternalPersonnel.FirstOrDefaultAsync(p => p.AzureUniqueId == personId.UniqueId),
                PersonId.IdentifierType.Mail => await resourcesDb.ExternalPersonnel.FirstOrDefaultAsync(p => p.Mail == personId.Mail),
                _ => throw new InvalidOperationException("Unsupported person identifier type")
            };

            return existingEntry;
        }

        public async Task<DbExternalPersonnelPerson> RefreshExternalPersonnelAsync(PersonId personId, bool considerRemovedProfile = false)
        {
            var profile = await ResolveProfileAsync(personId);

            if (profile == null && !considerRemovedProfile) //early check for null to avoid hitting DB unnecessary
                throw new PersonNotFoundError(personId.OriginalIdentifier);

            var resolvedPerson = await ResolveExternalPersonnelAsync(personId);

            if (resolvedPerson == null)
                throw new PersonNotFoundError(personId.OriginalIdentifier);

            bool hasChanged = false;

            if (profile != null)
            {
                if (resolvedPerson.AccountStatus != profile.GetDbAccountStatus() || resolvedPerson.AzureUniqueId != profile.AzureUniqueId || resolvedPerson.IsDeleted)
                {
                    resolvedPerson.AccountStatus = profile.GetDbAccountStatus();
                    resolvedPerson.AzureUniqueId = profile.AzureUniqueId;
                    resolvedPerson.JobTitle = profile.JobTitle;
                    resolvedPerson.Name = profile.Name;
                    resolvedPerson.Phone = profile.MobilePhone ?? string.Empty;
                    resolvedPerson.PreferredContractMail = profile.PreferredContactMail;
                    resolvedPerson.IsDeleted = false;

                    hasChanged = true;
                }
            }
            else
            {
                if (resolvedPerson.AccountStatus != DbAzureAccountStatus.NoAccount || resolvedPerson.IsDeleted == false && considerRemovedProfile)
                {
                    // Refreshed person exists in resources but not anymore as a valid profile in PEOPLE service
                    resolvedPerson.AccountStatus = DbAzureAccountStatus.NoAccount;
                    if (considerRemovedProfile)
                        resolvedPerson.IsDeleted = true;

                    hasChanged = true;
                }
            }

            if (hasChanged)
                await resourcesDb.SaveChangesAsync();

            return resolvedPerson;
        }

        public async Task<DbExternalPersonnelPerson> EnsureExternalPersonnelAsync(string mail, string firstName, string lastName)
        {
            // Should refactor this to distributed lock.

            await Locker.WaitAsync();

            try
            {
                var existingEntry = await ResolveExternalPersonnelAsync(mail);

                if (existingEntry != null)
                    return existingEntry;

                var profile = await ResolveProfileAsync(mail);

                var newEntry = new DbExternalPersonnelPerson()
                {
                    AccountStatus = DbAzureAccountStatus.NoAccount,
                    Disciplines = new List<DbPersonnelDiscipline>(),
                    Mail = mail,
                    Name = $"{firstName} {lastName}",
                    FirstName = firstName,
                    LastName = lastName,
                    Phone = string.Empty
                };

                if (profile != null)
                {
                    newEntry.Mail = profile.Mail ?? string.Empty;
                    newEntry.AccountStatus = profile.GetDbAccountStatus();
                    newEntry.AzureUniqueId = profile.AzureUniqueId;
                    newEntry.JobTitle = profile.JobTitle;
                    newEntry.Name = profile.Name;
                    newEntry.Phone = profile.MobilePhone ?? string.Empty;
                    newEntry.PreferredContractMail = profile.PreferredContactMail;
                }

                await resourcesDb.ExternalPersonnel.AddAsync(newEntry);

                // We  must save changes so next request can pick it up.
                await resourcesDb.SaveChangesAsync();

                return newEntry;

            }
            finally
            {
                Locker.Release();
            }
        }

        public async Task<DbPerson?> EnsurePersonAsync(PersonId personId)
        {
            await Locker.WaitAsync();

            try
            {
                FusionPersonProfile? profile;
                Guid personAzureUniqueId = personId.UniqueId.GetValueOrDefault();

                if (personId.Type == PersonId.IdentifierType.Mail)
                {
                    profile = await ResolveProfileAsync(personId);

                    if (profile != null) personAzureUniqueId = profile.AzureUniqueId.GetValueOrDefault();
                }

                if (personAzureUniqueId != Guid.Empty)
                {
                    var person = await resourcesDb.Persons.FirstOrDefaultAsync(p => p.AzureUniqueId == personAzureUniqueId);
                    if (person != null)
                        return person;
                }

                // Load profile into database
                profile = await ResolveProfileAsync(personId);

                if (profile == null)
                    return null;

                if (profile.AzureUniqueId == null)
                    throw new InvalidOperationException("Cannot ensure a person without an azure unique id");

                var newPerson = new DbPerson
                {
                    AccountType = profile.AccountType.ToString(),
                    AzureUniqueId = profile.AzureUniqueId.Value,
                    JobTitle = profile.JobTitle,
                    Mail = profile.Mail,
                    Name = profile.Name,
                    Phone = profile.MobilePhone
                };

                await resourcesDb.Persons.AddAsync(newPerson);
                await resourcesDb.SaveChangesAsync();

                return newPerson;
            }
            finally
            {
                Locker.Release();
            }
        }

        public async Task<DbPerson?> EnsureApplicationAsync(Guid azureUniqueId)
        {
            await Locker.WaitAsync();

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
                Locker.Release();
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
