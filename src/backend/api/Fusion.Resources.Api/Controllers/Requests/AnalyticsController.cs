using System.Linq;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Queries;


namespace Fusion.Resources.Api.Controllers
{
    [ApiVersion("1.0-preview")]
    [Authorize]
    [ApiController]
    public class AnalyticsController : ResourceControllerBase
    {

        [HttpGet("/analytics/requests/internal")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequestForAnalytics>>> GetAllRequests([FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.GlobalRoleAccess("Fusion.Analytics.Requests");
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var requestQuery = await DispatchAsync(new GetResourceAllocationRequestsForAnalytics(query));
            var apiModel = requestQuery.Select(x => new ApiResourceAllocationRequestForAnalytics(x)).ToList();
            var collection = new ApiCollection<ApiResourceAllocationRequestForAnalytics>(apiModel) { TotalCount = requestQuery.TotalCount };
            return collection;
        }

        [HttpGet("/analytics/absence/internal")]
        public async Task<ActionResult<ApiCollection<ApiPersonAbsenceForAnalytics>>> GetPersonsAbsence([FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                {
                    or.GlobalRoleAccess("Fusion.Analytics.Requests");
                });

            });
            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var allAbsenceQuery = await DispatchAsync(new GetPersonsAbsenceForAnalytics(query));
            var apiModel = allAbsenceQuery.Select(ApiPersonAbsenceForAnalytics.CreateWithoutConfidentialTaskInfo);

            var collection = new ApiCollection<ApiPersonAbsenceForAnalytics>(apiModel) { TotalCount = allAbsenceQuery.TotalCount };
            return collection;
        }
    }
}
