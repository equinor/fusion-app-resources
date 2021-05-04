using Fusion.Events;
using Fusion.Integration;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly IFusionProfileResolver profileResolver;
        private readonly IApiSubscriptionManager subscriptionManager;

        public SubscriptionsController(IFusionProfileResolver profileResolver, IApiSubscriptionManager subscriptionManager)
        {
            this.profileResolver = profileResolver;
            this.subscriptionManager = subscriptionManager;
        }

        [HttpPut("/subscriptions/internal-requests")]
        public async Task<IActionResult> RenewSubscription([FromBody] SubscriptionRequest request)
        {
            if (!User.IsApplicationUser())
            {
                return FusionApiError.Forbidden("Only applications can register subscriptions");
            }

            var appId = User.GetApplicationId();
            var userId = User.GetAzureUniqueIdOrThrow();

            var servicePrincipal = await profileResolver.ResolveServicePrincipalAsync(userId);

            if (!Enum.TryParse($"{request.Type}", out SubscriptionDetails.SubscriptionType subscriptionType))
                return FusionApiError.InvalidOperation("InvalidArgument", "The type is not valid.");

            var details = new SubscriptionDetails(subscriptionType, request.Identifier, appId, servicePrincipal!.DisplayName, request.Id);
            if (request.TypeFilter != null && request.TypeFilter.Length > 0)
                details.TypeFilter = request.TypeFilter;


            var connectionDetails = await subscriptionManager.EnsureSubscriptionAsync(details);

            return new OkObjectResult(new ApiEventSubscriptionV1(connectionDetails, "resources-sub"));
        }
        public class SubscriptionRequest
        {
            public Guid? Id { get; set; }
            public string? Identifier { get; set; }
            [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
            [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
            public ApiSubscriptionType? Type { get; set; }
            public string[]? TypeFilter { get; set; }

        }
    }


}
