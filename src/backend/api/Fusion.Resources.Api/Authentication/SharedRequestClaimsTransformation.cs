using Fusion.Integration.Authentication;
using Fusion.Integration.Profile;
using Fusion.Resources.Api.Authorization;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Authentication
{
    public class SharedRequestClaimsTransformation : ILocalClaimsTransformation
    {
        private static Task<IEnumerable<Claim>> noClaims = Task.FromResult<IEnumerable<Claim>>(Array.Empty<Claim>());
        private readonly ResourcesDbContext db;

        public SharedRequestClaimsTransformation(ResourcesDbContext db)
        {
            this.db = db;
        }

        public Task<IEnumerable<Claim>> TransformApplicationAsync(ClaimsPrincipal principal, FusionApplicationProfile profile)
        {
            return noClaims;
        }

        public async Task<IEnumerable<Claim>> TransformUserAsync(ClaimsPrincipal principal, FusionFullPersonProfile profile)
        {
            var claims = new List<Claim>();
            var sharedRequests = await db.SharedRequests
                .Where(x => x.SharedWith.AzureUniqueId == profile.AzureUniqueId)
                .ToListAsync();

            foreach(var sharedRequest in sharedRequests)
            {
                var claimType = $"{ResourcesClaimTypes.Prefix}{sharedRequest.Scope}";
                claims.Add(new Claim(claimType, sharedRequest.RequestId.ToString()));
            }

            return claims;
        }
    }
}
