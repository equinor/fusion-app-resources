using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Integration.Http;
using Fusion.Resources.Api.Integrations;
using Fusion.Resources.Domain;
using Itenso.TimePeriod;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        private readonly IHttpClientFactory httpClientFactory;
        public InternalPersonnelController(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        [HttpGet("departments/{fullDepartmentString}/resources/personnel")]
        public async Task<ActionResult<ApiCollection<ApiInternalPersonnelPerson>>> GetDepartmentPersonnel(string fullDepartmentString,
            [FromQuery] ODataQueryParams query,
            [FromQuery] DateTime? timelineStart = null,
            [FromQuery] string? timelineDuration = null,
            [FromQuery] DateTime? timelineEnd = null)
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

            // Departments are 
            //if (query is null) query = new ODataQueryParams { Top = 1000 };
            //if (query.Top > 1000) return ApiErrors.InvalidPageSize("Max page size is 1000");

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

        [HttpGet("sectors/{sectorString}/resources/personnel")]
        public async Task<ActionResult<ApiCollection<ApiSectorDepartments>>> GetSectorPersonnel(string SectorString,
            [FromQuery] ODataQueryParams query,
            [FromQuery] DateTime? timelineStart = null,
            [FromQuery] string? timelineDuration = null,
            [FromQuery] DateTime? timelineEnd = null)
        {
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();
                    or.FullControlInternal();
                    // TODO: Figure out auth requirements
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            //get departments from lineOrg
            var departmentStrings = await FindChildDepartments(SectorString);
            foreach (var departmentString in departmentStrings)
            {
                var department = await DispatchAsync(new GetDepartmentPersonnel(departmentString, query);
                // .WithTimeline(shouldExpandTimeline, timelineStart, timelineEnd));
            }
            //for each department, get personnel as above


            //example response
            //var sectorDepartments = new ApiSectorDepartments()
            //{
            //    SectorString = SectorString,
            //    DepartmentsInSector = new List<DepartmentInSector>()
            //    {
            //        new DepartmentInSector()
            //        {
            //            DepartmentString = "TEST DPT",
            //            DepartmentPersonnel = new List<ApiInternalPersonnelPerson>()
            //            {

            //            }
            //        }
            //    }

            //};
            return new ApiCollection<ApiSectorDepartments>(new List<ApiSectorDepartments>());
        }

        private async Task<List<string>> FindChildDepartments(string SectorString)
        {
            var lineOrgClient = httpClientFactory.CreateClient("LineOrg");

            var departmentStrings = new List<String>();
            var resource = $"/lineorg/departments/{SectorString}?api-version=1.0&$expand=children";
            var response = await lineOrgClient.GetAsync(resource);
            if (!response.IsSuccessStatusCode)
                  throw new LineOrgIntegrationError();
           
            var json = await response.Content.ReadAsStringAsync();
            //funker dette?
            var departments = JsonConvert.DeserializeObject<List<Department>>(json);
            // name eller fullname?
            departmentStrings.AddRange(departments.Select(department => department.FullName));
            return departmentStrings;
        }
        }
    }
}