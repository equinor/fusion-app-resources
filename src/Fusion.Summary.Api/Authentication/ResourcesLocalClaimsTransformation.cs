using Fusion.Integration.Authentication;
using Fusion.Integration.Profile;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Fusion.Integration.LineOrg;
using Fusion.Services.LineOrg.ApiModels;
using System.Text.RegularExpressions;
using Fusion.Summary.Api.Authorization;

namespace Fusion.Summary.Api.Authentication
{
    public class ResourcesLocalClaimsTransformation : ILocalClaimsTransformation
    {
        private static Task<IEnumerable<Claim>> noClaims = Task.FromResult<IEnumerable<Claim>>(Array.Empty<Claim>());
        private readonly ILogger<ResourcesLocalClaimsTransformation> logger;
        private readonly IMediator mediator;
        private readonly ILineOrgResolver lineOrgResolver;

        public ResourcesLocalClaimsTransformation(ILogger<ResourcesLocalClaimsTransformation> logger, IMediator mediator,
            ILineOrgResolver lineOrgResolver)
        {
            this.logger = logger;
            this.mediator = mediator;
            this.lineOrgResolver = lineOrgResolver;
        }

        public Task<IEnumerable<Claim>> TransformApplicationAsync(ClaimsPrincipal principal, FusionApplicationProfile profile)
        {
            return noClaims;
        }

        public async Task<IEnumerable<Claim>> TransformUserAsync(ClaimsPrincipal principal, FusionFullPersonProfile profile)
        {
            var claims = new List<Claim>();
            await ApplyResourceOwnerForDepartmentClaimIfUserIsResourceOwnerAsync(profile, claims);
            await ApplyDelegatedResourceOwnerForDepartmentClaimIfUserIsDelegatedResourceOwnerAsync(profile, claims);

            return claims;
        }

        private async Task ApplyDelegatedResourceOwnerForDepartmentClaimIfUserIsDelegatedResourceOwnerAsync(FusionFullPersonProfile profile, List<Claim> claims)
        {
            if (profile.Roles is null)
            {
                throw new InvalidOperationException("Roles must be loaded on the profile for the claims transformer to work.");
            }

            var delegatedRoles = profile.Roles
                .Where(x => string.Equals(x.Name, AccessRoles.ResourceOwner, StringComparison.OrdinalIgnoreCase))
                .Where(x => !string.IsNullOrEmpty(x.Scope?.Value))
                .Select(x => x.Scope?.Value!)
                .ToList();

            foreach (var delegatedRole in delegatedRoles)
            {
                ApiOrgUnit? orgUnit;
                try
                {
                    orgUnit = await ResolveLineOrgUnit(delegatedRole);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to resolve org unit {DelegatedRoleValue} for delegated resource owner", delegatedRole);
                    continue;
                }

                if (orgUnit?.FullDepartment != null)
                {
                    claims.Add(new Claim(ResourcesClaimTypes.DelegatedResourceOwnerForDepartment, orgUnit.FullDepartment));
                }
            }
        }

        private async Task ApplyResourceOwnerForDepartmentClaimIfUserIsResourceOwnerAsync(FusionFullPersonProfile profile, List<Claim> claims)
        {
            if (profile.Roles is null)
            {
                throw new InvalidOperationException("Roles must be loaded on the profile for the claims transformer to work.");
            }

            var managerRoles = profile.Roles
                .Where(x => string.Equals(x.Name, AccessRoles.LineOrgManager, StringComparison.OrdinalIgnoreCase))
                .Where(x => !string.IsNullOrEmpty(x.Scope?.Value))
                .Select(x => x.Scope?.Value!)
                .ToList();

            // Got a list of sap id's, need to resolve them to the full department to keep consistent.
            logger.LogDebug("Found user responsible for [{ManagerRolesCount}] org units [{Roles}]", managerRoles.Count, string.Join(",", managerRoles));

            foreach (var orgUnitId in managerRoles)
            {
                ApiOrgUnit? orgUnit;
                try
                {
                    orgUnit = await ResolveLineOrgUnit(orgUnitId);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to resolve org unit {OrgUnitId} for resource owner", orgUnitId);
                    continue;
                }

                if (orgUnit?.FullDepartment != null)
                {
                    claims.Add(new Claim(ResourcesClaimTypes.ResourceOwnerForDepartment, orgUnit.FullDepartment));
                    logger.LogDebug("Adding claim for {OrgUnitId} -> [{OrgUnitFullDepartment}]", orgUnitId, orgUnit.FullDepartment);
                }
            }
        }

        private async Task<ApiOrgUnit?> ResolveLineOrgUnit(string department)
        {
            if (string.IsNullOrEmpty(department))
                return null;

            var departmentId = Regex.IsMatch(department, @"\d+") ? Integration.LineOrg.DepartmentId.FromSapId(department)
                : Integration.LineOrg.DepartmentId.FromFullPath(department);

            var lineOrgDpt = await lineOrgResolver.ResolveOrgUnitAsync(departmentId);

            return lineOrgDpt;
        }
    }
}