using Fusion.Integration.Profile;
using Fusion.Resources.Database.Entities;
using System;
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

        Task<DbPerson?> EnsureApplicationAsync(Guid azureUniqueId);

        Task<DbExternalPersonnelPerson> EnsureExternalPersonnelAsync(string? upn, string mail, string firstName, string lastName);
        Task<DbExternalPersonnelPerson?> ResolveExternalPersonnelAsync(PersonId personId);
        /// <summary>
        /// Resolves the fusion profile. Returns null if not found.
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        Task<FusionPersonProfile?> ResolveProfileAsync(PersonId person);

        /// <summary>
        /// Refresh external personnel information from People services. 
        /// Will add it to database if not present.
        /// </summary>
        /// <param name="personId">Identifier for person, can be e-mail or azure unique id</param>
        /// <param name="considerRemovedProfile">Refresh will proceed even if unable to resolve profile from people service. Profile still may exist in external personnel</param>
        /// <returns>The updated person entity</returns>
        Task<DbExternalPersonnelPerson> RefreshExternalPersonnelAsync(PersonId personId, bool considerRemovedProfile = false);
    }
}
