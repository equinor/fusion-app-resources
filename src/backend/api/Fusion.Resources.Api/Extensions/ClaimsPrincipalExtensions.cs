using Fusion.Integration;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Fusion.Resources.Api
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Workday version. Use this instead of User.GetResponsibleForDepartments() in integration lib.
        /// </summary>
        public static IEnumerable<string> GetManagerForDepartments(this ClaimsPrincipal user) => user.FindAll(ResourcesClaimTypes.ResourceOwnerForDepartment)
            .Select(c => c.Value);
    }
}
