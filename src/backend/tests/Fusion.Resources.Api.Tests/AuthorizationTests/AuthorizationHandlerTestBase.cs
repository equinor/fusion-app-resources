using Fusion.Integration;
using System;
using System.Security.Claims;

namespace Fusion.Resources.Api.Tests
{
    public class AuthorizationHandlerTestBase
    {

        protected ClaimsPrincipal GetClaimsUser(Guid userUniqueId, Action<ClaimsIdentity> builder = null)
        {
            var identity = new ClaimsIdentity();
            builder?.Invoke(identity);

            identity.AddClaim(new Claim(FusionClaimsTypes.AzureUniquePersonId, $"{userUniqueId}"));

            return new ClaimsPrincipal(identity);
        }

    }
}
