using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using System.Collections.Concurrent;

#nullable enable
namespace Fusion.Resources.Domain.Services
{
    internal class ProfileServices : IProfileService
    {
        private readonly IFusionProfileResolver profileResolver;
        private readonly ResourcesDbContext resourcesDb;
        private readonly TelemetryClient telemetryClient;
        private static readonly SemaphoreSlim locker = new SemaphoreSlim(1);

        public ProfileServices(IFusionProfileResolver profileResolver, ResourcesDbContext resourcesDb, TelemetryClient telemetryClient)
        {
            this.profileResolver = profileResolver;
            this.resourcesDb = resourcesDb;
            this.telemetryClient = telemetryClient;
        }

        public async Task<DbExternalPersonnelPerson?> ResolveExternalPersonnelAsync(PersonId personId)
        {
            // In most cases the same mail could refer to different persons, since the equinor mails are reused, however
            // since this is external ids, we could assume that the mails are not reused.

            var existingEntry = personId.Type switch
            {
                PersonId.IdentifierType.UniqueId => await resourcesDb.ExternalPersonnel.FirstOrDefaultAsync(p => p.AzureUniqueId == personId.UniqueId),
                PersonId.IdentifierType.Mail => await resourcesDb.ExternalPersonnel.FirstOrDefaultAsync(p => p.Mail == personId.Mail && p.IsDeleted != true),
                _ => throw new InvalidOperationException("Unsupported person identifier type")
            };

            return existingEntry;
        }

        public async Task<DbExternalPersonnelPerson> RefreshExternalPersonnelAsync(PersonId personId, bool considerRemovedProfile = false)
        {
            // Make sure we don't get fooled by cached profiles.
            using var noCacheScope = new NoProfileCacheScope();
            var profile = await ResolveProfileAsync(personId);

            if (profile == null && !considerRemovedProfile) //early check for null to avoid hitting DB unnecessary
                throw new PersonNotFoundError(personId.OriginalIdentifier);

            var resolvedPerson = await ResolveExternalPersonnelAsync(personId);

            if (resolvedPerson == null)
                throw new PersonNotFoundError(personId.OriginalIdentifier);

            // Profile found in people service
            if (profile != null)
            {
                //New external personnel without azureUniqueId, should be provided azureUniqueId if found in people service
                if (!resolvedPerson.AzureUniqueId.HasValue && profile.AzureUniqueId.HasValue)
                {
                    resolvedPerson.AzureUniqueId = profile.AzureUniqueId;
                }

                //Existing external personnel should be matched by azureUniqueId
                if (resolvedPerson.AzureUniqueId == profile.AzureUniqueId)
                {
                    resolvedPerson.AccountStatus = profile.GetDbAccountStatus();
                    resolvedPerson.UPN = profile.UPN;
                    resolvedPerson.JobTitle = profile.JobTitle;
                    resolvedPerson.Name = profile.Name;
                    resolvedPerson.Phone = profile.MobilePhone ?? string.Empty;
                    resolvedPerson.PreferredContractMail = profile.PreferredContactMail;
                    resolvedPerson.IsDeleted = profile.IsExpired;
                    resolvedPerson.Deleted = profile.ExpiredDate;
                }
            }
            else
            {
                // Refreshed person exists in external personnel but not found as a valid profile in PEOPLE service
                resolvedPerson.AccountStatus = DbAzureAccountStatus.NoAccount;
                if (considerRemovedProfile && resolvedPerson.AzureUniqueId.HasValue)
                {
                    resolvedPerson.IsDeleted = true;
                    resolvedPerson.Deleted = DateTimeOffset.UtcNow;
                }
            }

            var changedProperties = resourcesDb.Entry(resolvedPerson).Properties
                .Where(x => x.IsModified)
                .ToList();

            if (!changedProperties.Any()) return resolvedPerson;

            telemetryClient.TrackTrace($"Updated properties for user {personId.OriginalIdentifier} : {JsonConvert.SerializeObject(changedProperties.Select(x => new { PropertyName = x.Metadata.Name, OriginalValue = x.OriginalValue, CurrentValue = x.CurrentValue }), Formatting.Indented)}");
            await resourcesDb.SaveChangesAsync();

            return resolvedPerson;
        }

        public async Task<DbExternalPersonnelPerson> EnsureExternalPersonnelAsync(string? upn, PersonId personIdentifier, string firstName, string lastName)
        {
            // Should refactor this to distributed lock.

            await locker.WaitAsync();

            try
            {
                var existingEntry = await ResolveExternalPersonnelAsync(personIdentifier);

                if (existingEntry != null)
                    return existingEntry;

                var profile = await ResolveProfileAsync(personIdentifier);

                var newEntry = new DbExternalPersonnelPerson
                {
                    AccountStatus = DbAzureAccountStatus.NoAccount,
                    Disciplines = new List<DbPersonnelDiscipline>(),
                    UPN = upn,
                    Mail = string.Empty,
                    Name = $"{firstName} {lastName}",
                    FirstName = firstName,
                    LastName = lastName,
                    Phone = string.Empty
                };

                switch (personIdentifier.Type)
                {
                    case PersonId.IdentifierType.UniqueId:
                        newEntry.AzureUniqueId = personIdentifier.UniqueId;
                        break;
                    case PersonId.IdentifierType.Mail:
                        newEntry.Mail = personIdentifier.Mail!;
                        break;
                }

                if (profile != null)
                {
                    newEntry.UPN = profile.UPN;
                    newEntry.Mail = profile.Mail ?? newEntry.Mail;
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
                locker.Release();
            }
        }

        public async Task<DbPerson?> EnsurePersonAsync(PersonId personId)
        {
            await locker.WaitAsync();

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
                locker.Release();
            }
        }

        public async Task<List<DbPerson>> EnsurePersonsAsync(IEnumerable<PersonId> personIds)
        {
            var resolved = await ResolveProfilesAsync(personIds);
            PersonsNotFoundError.ThrowWhenAnyFailed(resolved);

            await locker.WaitAsync();
            try
            {
                var ensuredPersons = new List<DbPerson>();
                foreach (var profile in resolved!.Select(x => x.Profile))
                {
                    if (profile?.AzureUniqueId == null)
                        throw new InvalidOperationException("Cannot ensure a person without an azure unique id");

                    var dbPerson = await resourcesDb.Persons.FirstOrDefaultAsync(x => x.AzureUniqueId == profile.AzureUniqueId);
                    if (dbPerson is null) dbPerson = new DbPerson();

                    dbPerson.AccountType = profile.AccountType.ToString();
                    dbPerson.AzureUniqueId = profile.AzureUniqueId.Value;
                    dbPerson.JobTitle = profile.JobTitle;
                    dbPerson.Mail = profile.Mail;
                    dbPerson.Name = profile.Name;
                    dbPerson.Phone = profile.MobilePhone;

                    ensuredPersons.Add(dbPerson);
                }

                resourcesDb.Persons.AddRange(ensuredPersons);
                await resourcesDb.SaveChangesAsync();

                return ensuredPersons;
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

        public Task<IEnumerable<ResolvedPersonProfile>?> ResolveProfilesAsync(IEnumerable<PersonId> personIds)
        {
            return ResolveProfilesAsync(personIds.Select(x => (PersonIdentifier)x));
        }

        public async Task<IEnumerable<ResolvedPersonProfile>?> ResolveProfilesAsync(IEnumerable<PersonIdentifier> personIds)
        {
            try
            {
                var resolved = new List<ResolvedPersonProfile>();
                var chunked = Partitioner.Create(0, personIds.Count(), 500);
                foreach ((int low, int high) in chunked.GetDynamicPartitions())
                {
                    resolved.AddRange(
                        await profileResolver.ResolvePersonsAsync(personIds.Skip(low).Take(high))
                    );
                }
                return resolved;
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
