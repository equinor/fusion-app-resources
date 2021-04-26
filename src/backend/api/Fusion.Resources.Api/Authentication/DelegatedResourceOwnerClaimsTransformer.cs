using Fusion.Integration;
using Fusion.Integration.Authentication;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Authentication
{
    public class DelegatedResourceOwnerClaimsTransformer : ILocalClaimsTransformation
    {

        // Cache the empty task instead of creating new ref each time
        private static Task<IEnumerable<Claim>> noClaims = Task.FromResult<IEnumerable<Claim>>(Array.Empty<Claim>());
        private readonly ResourcesDbContext dbContext;

        public DelegatedResourceOwnerClaimsTransformer(ResourcesDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public Task<IEnumerable<Claim>> TransformApplicationAsync(ClaimsPrincipal principal, FusionApplicationProfile profile)
        {
            return noClaims;
        }

        public async Task<IEnumerable<Claim>> TransformUserAsync(ClaimsPrincipal principal, FusionFullPersonProfile profile)
        {
            var userAzureId = principal.GetAzureUniqueId();
            if (userAzureId is null)
                return Array.Empty<Claim>();

            var userDepartments = await dbContext.DepartmentResponsibles
                .Where(d => d.ResponsibleAzureObjectId == userAzureId && d.DateFrom < DateTime.UtcNow && d.DateTo > DateTime.UtcNow)
                .ToListAsync();

            return userDepartments.Select(d => new Claim(FusionClaimsTypes.ResourceOwner, d.DepartmentId));
        }
    }
}
