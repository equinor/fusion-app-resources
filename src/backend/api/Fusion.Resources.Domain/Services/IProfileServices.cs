using Fusion.Integration.Profile;
using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public interface IProfileService
    {
        /// <summary>
        /// The person entity is a user that can sign in and perform actions. 
        /// In this case using the mail can be ambiguous, due to reuse with x-amount of years etc.
        /// </summary>
        /// <param name="personId">The person identifier. Can be either mail or azure unique id. If mail, the profile will be resolved to an azure unique id.</param>
        /// <returns></returns>
        Task<DbPerson?> EnsurePersonAsync(PersonId personId);
        /// <summary>
        /// Looks up all the person identifiers in the Fusion People service and adds or updates the local database person entities.
        /// </summary>
        /// <param name="personIds">The Email adresses or Azure AD object ids to lookup, can contain a mix of both types.</param>
        /// <returns>The updated list of local database entities</returns>
        Task<List<DbPerson>> EnsurePersonsAsync(IEnumerable<PersonId> personIds);

        Task<DbPerson?> EnsureApplicationAsync(Guid azureUniqueId);

        Task<IEnumerable<ResolvedPersonProfile>?> ResolveProfilesAsync(IEnumerable<PersonId> personIds);
        Task<IEnumerable<ResolvedPersonProfile>?> ResolveProfilesAsync(IEnumerable<PersonIdentifier> personIds);

        /// <summary>
        /// Resolves the fusion profile. Returns null if not found.
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        Task<FusionPersonProfile?> ResolveProfileAsync(PersonId person);
        Task<DbPerson> EnsureSystemAccountAsync();
    }
}
