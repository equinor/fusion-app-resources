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
        /// In this case using the mail can be ambigous, due to reuse with x-amount of years etc.
        /// </summary>
        /// <param name="azureUniqueId"></param>
        /// <returns></returns>
        Task<DbPerson?> EnsurePersonAsync(Guid azureUniqueId);

        Task<DbPerson?> EnsureApplicationAsync(Guid azureUniqueId);

        Task<DbExternalPersonnelPerson> EnsureExternalPersonnelAsync(PersonId personId);
        Task<DbExternalPersonnelPerson?> ResolveExternalPersonnelAsync(PersonId personId);
        /// <summary>
        /// Resolves the fusion profile. Returns null if not found.
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        Task<FusionPersonProfile?> ResolveProfileAsync(PersonId person);
    }
}
