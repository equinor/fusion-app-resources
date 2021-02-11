using System.Linq;
using System.Threading.Tasks;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class DepartmentRequestController : ResourceControllerBase
    {
        [HttpGet("/departments/{fullDepartmentString}/resources/requests")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetAssignedDepartmentRequests(string fullDepartmentString, ODataQueryParams? query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or => { });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var requests = await DispatchAsync(
                new GetResourceAllocationRequests(query)
                    .WithAssignedDepartment(fullDepartmentString)
                );

            return new ApiCollection<ApiResourceAllocationRequest>(requests.Select(x => new ApiResourceAllocationRequest(x)));
        }
    }
}