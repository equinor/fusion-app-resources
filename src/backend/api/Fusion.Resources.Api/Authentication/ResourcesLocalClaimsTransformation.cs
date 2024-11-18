using Fusion.Integration.Authentication;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using Fusion.Resources.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ResourcesLocalClaimsTransformation> logger;
        private readonly ResourcesDbContext db;
        private readonly IMediator mediator;

        public ResourcesLocalClaimsTransformation(ILogger<ResourcesLocalClaimsTransformation> logger, ResourcesDbContext db, IMediator mediator)
        {
            this.logger = logger;
            this.db = db;
            this.mediator = mediator;
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

        private async Task ApplyResourceOwnerForDepartmentClaimIfUserIsResourceOwnerAsync(FusionFullPersonProfile profile, List<Claim> claims)
        {
            // This will now point to incorrect department. We need to use the roles on the profile, to see scoped manager responsebility.
            // Leaving in for reference.
            //if (profile.IsResourceOwner && !string.IsNullOrEmpty(profile.FullDepartment))
            //{
            //    claims.Add(new Claim(ResourcesClaimTypes.ResourceOwnerForDepartment, profile.FullDepartment));
            //}

            if (profile.Roles is null) {
                throw new InvalidOperationException("Roles must be loaded on the profile for the claims transformer to work.");
            }

            var managerRoles = profile.Roles
                .Where(x => string.Equals(x.Name, "Fusion.LineOrg.Manager", StringComparison.OrdinalIgnoreCase))
                .Where(x => !string.IsNullOrEmpty(x.Scope?.Value))
                .Select(x => x.Scope?.Value!)
                .ToList();

            // Got a list of sap id's, need to resolve them to the full department to keep consistent.
            logger.LogInformation($"Found user responsible for [{managerRoles.Count}] org units [{string.Join(",", managerRoles)}]");

            foreach (var orgUnitId in managerRoles)
            {
                var orgUnit = await mediator.Send(new ResolveLineOrgUnit(orgUnitId));
                if (orgUnit?.FullDepartment != null)
                {
                    claims.Add(new Claim(ResourcesClaimTypes.ResourceOwnerForDepartment, orgUnit.FullDepartment));
                    logger.LogInformation($"Adding claim for {orgUnitId} -> [{orgUnit.FullDepartment}]");
                }
            }
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
