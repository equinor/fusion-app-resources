using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Fusion.Resources.Api.Authorization;

namespace Fusion.Resources.Api.Controllers
{
    [ApiVersion("1.0")]
    [Authorize]
    [ApiController]
    public class AnalyticsController : ResourceControllerBase
    {

        [HttpGet("/analytics/requests/internal")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetAllRequests([FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.ScopeAccess(ScopeAccess.QueryAnalyticsRequests);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion
            var requestCommand = new GetResourceAllocationRequestsForAnalytics(query);
            var result = await DispatchAsync(requestCommand);

            var apiModel = result.Select(x => new ApiResourceAllocationRequest(x)).ToList();
            return new ApiCollection<ApiResourceAllocationRequest>(apiModel);
        }
    }
}
