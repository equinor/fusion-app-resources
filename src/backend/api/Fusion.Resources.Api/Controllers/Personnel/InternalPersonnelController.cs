using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Integration.Http;
using Fusion.Resources.Domain;
using Itenso.TimePeriod;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace Fusion.Resources.Api.Controllers
{
    [ApiVersion("1.0-preview")]
    [Authorize]
    [ApiController]
    public class InternalPersonnelController : ResourceControllerBase
    {

        public InternalPersonnelController()
        {
        }

        [HttpGet("departments/{fullDepartmentString}/resources/personnel")]
        public async Task<ActionResult<ApiCollection<ApiInternalPersonnelPerson>>> GetDepartmentPersonnel(string fullDepartmentString, 
            [FromQuery] ODataQueryParams query, 
            [FromQuery]DateTime? timelineStart = null, 
            [FromQuery]string? timelineDuration = null, 
            [FromQuery]DateTime? timelineEnd = null)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();

                    or.FullControlInternal();

                    // TODO add
                    // - Resource owner in line org chain (all departments upwrards)
                    // - Is resource owner in general (?)
                    // - Fusion.Resources.Department.ReadAll in any department scope upwards in line org.
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            #region Validate input if timeline is expanded

            var shouldExpandTimeline = query.ShoudExpand("timeline");
            if (shouldExpandTimeline)
            {
                if (timelineStart is null)
                    return ApiErrors.MissingInput(nameof(timelineStart), "Must specify 'timelineStart' when expanding timeline");

                TimeSpan? duration;

                try { duration = timelineDuration != null ? XmlConvert.ToTimeSpan("P5M") : null; }
                catch (Exception ex)
                {
                    return ApiErrors.InvalidInput("Invalid duration value: " + ex.Message);
                }

                if (timelineEnd is null)
                {
                    if (duration is null)
                        return ApiErrors.MissingInput(nameof(timelineDuration), "Must specify either 'timelineDuration' or 'timelineEnd' when expanding timeline");

                    timelineEnd = timelineStart.Value.Add(duration.Value);
                }
            }

            #endregion


            var department = await DispatchAsync(new GetDepartmentPersonnel(fullDepartmentString, query)
                .WithTimeline(shouldExpandTimeline, timelineStart, timelineEnd));


            var returnModel = department.Select(p => new ApiInternalPersonnelPerson(p)).ToList();
            return new ApiCollection<ApiInternalPersonnelPerson>(returnModel);
        }
    }

}
