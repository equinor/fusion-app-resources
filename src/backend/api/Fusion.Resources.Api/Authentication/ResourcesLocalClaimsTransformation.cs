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
    public class ResourcesLocalClaimsTransformation : ILocalClaimsTransformation
    {
        private static Task<IEnumerable<Claim>> noClaims = Task.FromResult<IEnumerable<Claim>>(Array.Empty<Claim>());
        private readonly ResourcesDbContext db;

        public ResourcesLocalClaimsTransformation(ResourcesDbContext db)
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
            await ApplySharedRequestClaimsIfAnyAsync(profile, claims);
            await ApplyResourceOwnerForDepartmentClaimIfUserIsResourceOwnerAsync(profile, claims);

            return claims;
        }

        private static Task ApplyResourceOwnerForDepartmentClaimIfUserIsResourceOwnerAsync(FusionFullPersonProfile profile, List<Claim> claims)
        {
            if (profile.IsResourceOwner && !string.IsNullOrEmpty(profile.FullDepartment))
            {
                claims.Add(new Claim(ResourcesClaimTypes.ResourceOwnerForDepartment, profile.FullDepartment));
            }

            return Task.CompletedTask;
        }

        private async Task ApplySharedRequestClaimsIfAnyAsync(FusionFullPersonProfile profile, List<Claim> claims)
        {
            var sharedRequests = await db.SharedRequests
                .Where(x => x.SharedWith.AzureUniqueId == profile.AzureUniqueId)
                .ToListAsync();

            foreach (var sharedRequest in sharedRequests)
            {
                var claimType = $"{ResourcesClaimTypes.Prefix}{sharedRequest.Scope}";
                claims.Add(new Claim(claimType, sharedRequest.RequestId.ToString()));
            }
        }
    }
}
