using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Integration.Configuration;
using Fusion.Integration.Http;
using Fusion.Resources.Api.Controllers.Departments;
using Fusion.Resources.Api.Integrations;
using Fusion.Resources.Database;
using Fusion.Resources.Domain;
using Itenso.TimePeriod;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private readonly ResourcesDbContext db;
        private readonly IFusionTokenProvider tokenProvider;
        public InternalPersonnelController(IHttpClientFactory httpClientFactory, ResourcesDbContext db, IFusionTokenProvider tokenProvider)
        {
            this.httpClientFactory = httpClientFactory;
            this.db = db;
            this.tokenProvider = tokenProvider;
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
        public async Task<ActionResult<ApiDepartment>> GetSectorPersonnel(string sectorString,
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

            var sector = await db.Departments
                .SingleOrDefaultAsync(dpt => dpt.OrgPath == sectorString && dpt.OrgType == OrgTypes.Sector.ToDbType());
            if (sector == null) return NotFound();
            
            var apiSector = new ApiDepartment(sector);

            var departmentStrings = await FindChildDepartments(sectorString);
            foreach (var departmentString in departmentStrings)
            {
                var department = await db.Departments
                .SingleOrDefaultAsync(dpt => dpt.OrgPath == departmentString && dpt.OrgType == OrgTypes.Department.ToDbType());
                if (department != null)
                {
                    var apiDepartment = new ApiDepartment(department);
                    var departmentPersonnel = await DispatchAsync(new GetDepartmentPersonnel(departmentString, query));
                    // .WithTimeline(shouldExpandTimeline, timelineStart, timelineEnd));
                    apiDepartment.DepartmentPersonell?.AddRange(departmentPersonnel.Select(p => new ApiInternalPersonnelPerson(p)).ToList());

                    apiSector.Children?.Add(apiDepartment);
                }
            }
            return apiSector;
        }

        private async Task<List<string>> FindChildDepartments(string SectorString)
        {
            var lineOrgClient = httpClientFactory.CreateClient("LineOrg");
            var token = await tokenProvider.GetApplicationTokenAsync();
            lineOrgClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

            var departmentStrings = new List<String>();
            var resource = $"/lineorg/departments/{SectorString}?api-version=1.0&$expand=children";
            var response = await lineOrgClient.GetAsync(resource);
            if (!response.IsSuccessStatusCode)
                  throw new LineOrgIntegrationError();

            var json = await response.Content.ReadAsStringAsync();

            var sector = JsonConvert.DeserializeObject<Department>(json);
  
            //var departments = new List<Department>()
            //{
            //    new Department() { Name = "PRD FE EA", FullName = "TPD PRD FE EA" },
            //    new Department() { Name = "PRD FE EM", FullName = "TPD PRD FE EM" }
            //};

            departmentStrings.AddRange(sector.Children.Select(child => child.FullName));
            return departmentStrings;
        }
    }
}