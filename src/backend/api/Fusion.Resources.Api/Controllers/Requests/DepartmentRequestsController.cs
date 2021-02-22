using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers.Requests
{
    [Authorize]
    [ApiController]
    public class DepartmentRequestsController : ResourceControllerBase
    {
        [HttpGet("departments/{departmentString}/resources/requests")]
        public async Task<ActionResult<ApiCollection<ApiResourceAllocationRequest>>> GetDepartmentRequests(
            [FromRoute] string departmentString, 
            [FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AnyOf(or =>
                {
                    // add requirements
                });
            });
            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var requestCommand = new GetResourceAllocationRequests(query);
            requestCommand.WithAssignedDepartment(departmentString);
            var result = await DispatchAsync(requestCommand);

            var apiModel = result.Select(x => new ApiResourceAllocationRequest(x)).ToList();
            return new ApiCollection<ApiResourceAllocationRequest>(apiModel);
        }
    }
}
