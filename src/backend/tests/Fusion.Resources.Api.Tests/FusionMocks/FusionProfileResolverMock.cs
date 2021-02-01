using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    public class FusionProfileResolverMock : IFusionProfileResolver
    {
        internal static readonly List<FusionFullPersonProfile> Profiles = new List<FusionFullPersonProfile>();
        public async Task<FusionFullPersonProfile> GetCurrentUserFullProfileAsync(Guid? azureUniqueUserId = null)
        {
            return Profiles.FirstOrDefault(x => x.AzureUniqueId == azureUniqueUserId);
        }

        public Task<FusionPersonProfile> GetCurrentUserBasicProfileAsync(Guid? azureUniqueUserId = null, bool noCache = false)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ResolvedPersonProfile>> ResolvePersonsAsync(IEnumerable<PersonIdentifier> personIdentifier)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FusionPersonProfile>> QueryPersonsAsync(string query)
        {
            throw new NotImplementedException();
        }

        public async Task<FusionPersonProfile?> ResolvePersonBasicProfileAsync(PersonIdentifier personIdentifier)
        {
            return Profiles.FirstOrDefault(x => x.AzureUniqueId == personIdentifier.AzureUniquePersonId);
        }

        public Task<FusionFullPersonProfile?> ResolvePersonFullProfileAsync(PersonIdentifier personIdentifier)
        {
            throw new NotImplementedException();
        }

        public Task<List<FusionRole>> ResolveFusionRolesAsync(PersonIdentifier personIdentifier)
        {
            throw new NotImplementedException();
        }

        public Task<FusionApplicationProfile?> ResolveServicePrincipalAsync(Guid azureUniquePersonId, bool expandRoles = false)
        {
            throw new NotImplementedException();
        }

        public Task<FusionApplicationProfile> ResolveApplicationAsync(Guid clientId, bool expandRoles = false)
        {
            throw new NotImplementedException();
        }


        public void AddProfile(ApiPersonProfileV3 user)
        {
            var profileFound = Profiles.Any(x => x.AzureUniqueId == user.AzureUniqueId);

            if (!profileFound)
                Profiles.Add(new FusionFullPersonProfile(user));
            
        }
    }
}