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
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using Fusion.Integration.Configuration;

#nullable enable
namespace Fusion.Resources.Domain.Services
{
    internal class ProfileServices : IProfileService
    {
        private readonly IFusionProfileResolver profileResolver;
        private readonly ResourcesDbContext resourcesDb;
        private readonly IFusionTokenProvider tokenProvider;
        private static readonly SemaphoreSlim locker = new SemaphoreSlim(1);

        public ProfileServices(IFusionProfileResolver profileResolver, ResourcesDbContext resourcesDb, IFusionTokenProvider tokenProvider)
        {
            this.profileResolver = profileResolver;
            this.resourcesDb = resourcesDb;
            this.tokenProvider = tokenProvider;
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
            var resolvedProfiles = await ResolveProfilesAsync(personIds);
            PersonsNotFoundError.ThrowWhenAnyFailed(resolvedProfiles);

            await locker.WaitAsync();
            try
            {
                var ensuredPersons = new List<DbPerson>();
                foreach (var resolved in resolvedProfiles!.Select(x => x.Profile).ToLookup(x => x!.AzureUniqueId))
                {
                    // avoid adding duplicate persons.
                    var profile = resolved.FirstOrDefault();
                    if (profile?.AzureUniqueId == null)
                        throw new InvalidOperationException("Cannot ensure a person without an azure unique id");

                    var dbPerson = await resourcesDb.Persons.FirstOrDefaultAsync(x => x.AzureUniqueId == profile.AzureUniqueId);
                    if (dbPerson is null)
                    {
                        dbPerson = new DbPerson();
                        resourcesDb.Persons.Add(dbPerson);
                    }

                    dbPerson.AccountType = profile.AccountType.ToString();
                    dbPerson.AzureUniqueId = profile.AzureUniqueId.Value;
                    dbPerson.JobTitle = profile.JobTitle;
                    dbPerson.Mail = profile.Mail;
                    dbPerson.Name = profile.Name;
                    dbPerson.Phone = profile.MobilePhone;

                    ensuredPersons.Add(dbPerson);
                }
                
                await resourcesDb.SaveChangesAsync();

                return ensuredPersons;
            }
            finally
            {
                locker.Release();
            }
        }

        public async Task<DbPerson> EnsureLocalSystemAccountAsync()
        {
            await locker.WaitAsync();

            try
            {
                var person = await resourcesDb.Persons.FirstOrDefaultAsync(p => p.AzureUniqueId == Guid.Empty);
                if (person != null)
                    return person;


                person = new DbPerson
                {
                    AccountType = $"{FusionAccountType.Application}",
                    AzureUniqueId = Guid.Empty,
                    JobTitle = "System Account",
                    Mail = $"system@FRA",
                    Name = "FRA System Account",
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

        public async Task<DbPerson> EnsureSystemAccountAsync()
        {
            var token = await tokenProvider.GetApplicationTokenAsync();
            var securityToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

            var claim = securityToken.Claims.FirstOrDefault(x => x.Type == "oid");
            if (claim == null)
                throw new InvalidOperationException("Could not locate azure unique object id claim");

            if (!Guid.TryParse(claim.Value, out Guid azureUniqueId) || azureUniqueId == Guid.Empty)
                throw new InvalidOperationException("Azure object id claim does not contain valid Guid");

            return await EnsureApplicationAsync(azureUniqueId) ?? throw new InvalidOperationException("Could not determine system account");
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
