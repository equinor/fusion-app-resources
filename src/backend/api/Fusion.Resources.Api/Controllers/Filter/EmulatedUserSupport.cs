using Fusion.Integration.Http.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Fusion.Integration.Http.Models;
using Azure.Core;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Integration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;


namespace Fusion.Resources.Api.Controllers
{
    /// <summary>
    /// Enables support for overriding the HttpContext.User object with a claims principal constructed using a provided user identifier. 
    /// 
    /// The user id is looked for in "?emulatedUserId" query param. Can be wither UPN or azure id.
    /// 
    /// Requires admin access to enter user emulation mode.
    /// 
    /// SHOULD ONLY BE ADDED TO ENDPOINTS THAT DOES NOT PROVIDE DATA ACCESS, EITHER READ OR WRITE!
    /// </summary>
    public class EmulatedUserSupport : ActionFilterAttribute
    {

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var emulatedUserId = context.HttpContext.Request.Query["emulatedUserId"];

            if (!string.IsNullOrEmpty(emulatedUserId))
            {
                var authorizationService = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
                var authResult = await context.HttpContext.Request.RequireAuthorizationAsync(r =>
                {
                    r.AlwaysAccessWhen().GlobalRoleAccess("Fusion.Resources.FullControl");
                    r.AlwaysAccessWhen().GlobalRoleAccess("Fusion.Resources.EmulateUser");
                });

                if (authResult.Unauthorized)
                {
                    context.Result = authResult.CreateForbiddenResponse();
                }


                var user = await context.HttpContext.GetEmulatedClaimsUserAsync(emulatedUserId!);

                context.HttpContext.User = user;
            }


            await base.OnActionExecutionAsync(context, next);
        }

    }

    public static class RequestExtensions
    {

        /// <summary>
        /// Should generate a mostly accurate claims principal based on the user id. 
        /// Will be missing role claims directly connected to the user through azure ad app registration roles.
        /// </summary>
        /// <param name="httpContext">HttpContext to fetch required services</param>
        /// <param name="userId">upn or azure unique id. Used to resolve initial profile</param>
        /// <returns>ClaimsPrincipal with standard fusion claims</returns>
        public static async Task<ClaimsPrincipal> GetEmulatedClaimsUserAsync(this HttpContext httpContext, string userId)
        {
            var resolver = httpContext.RequestServices.GetRequiredService<IFusionProfileResolver>();
            var claimsTransformer = httpContext.RequestServices.GetRequiredService<IClaimsTransformation>();

            var userProfile = await resolver.ResolvePersonBasicProfileAsync(userId);

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(FusionClaimsTypes.AzureUniquePersonId, $"{userProfile.AzureUniqueId}"),
                new Claim(ClaimTypes.Name, $"{userProfile.Name}"),
                new Claim("name", $"{userProfile.Name}"),
                new Claim("unique_name", userProfile.UPN),
                new Claim("upn", userProfile.UPN)
            }, "emulator");

            var user = new ClaimsPrincipal(identity);

            // Need to remove this, might affect rest of the request. 
            // The claimstransformer will just add the profile here, which will cause an exception when the key is already added.
            // Not much is using this item, so should be rather safe. 
            // TODO: Deprecate this in the integration lib.
            httpContext.Items.Remove("FusionProfile");

            await claimsTransformer.TransformAsync(user);

            return user;
        }
    }

}

