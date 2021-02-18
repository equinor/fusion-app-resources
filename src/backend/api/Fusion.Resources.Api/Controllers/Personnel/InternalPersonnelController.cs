using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Integration.Configuration;
using Fusion.Integration.Http;
using Fusion.Resources.Api.Controllers.Personnel.ApiModels;
using Fusion.Resources.Domain;
using Itenso.TimePeriod;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using static Fusion.Resources.Api.Controllers.Personnel.ApiModels.ApiSector;

namespace Fusion.Resources.Api.Controllers
{
    [ApiVersion("1.0-preview")]
    [Authorize]
    [ApiController]
    public class InternalPersonnelController : ResourceControllerBase
    {

        private readonly IHttpClientFactory httpClientFactory;
        private readonly IFusionTokenProvider tokenProvider;
        public InternalPersonnelController(IHttpClientFactory httpClientFactory, IFusionTokenProvider tokenProvider)
        {
            this.httpClientFactory = httpClientFactory;
            this.tokenProvider = tokenProvider;
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

        [HttpGet("sectors/{sectorString}/resources/personnel")]
        public async Task<ActionResult<ApiSector>> GetSectorPersonnel(string sectorString,
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

            var apiSector = new ApiSector(sectorString);
            var departmentStrings = await FindChildDepartments(sectorString);
            
            foreach (var departmentString in departmentStrings)
            {
                var departmentPersonnel = await DispatchAsync(new GetDepartmentPersonnel(departmentString, query));
                // .WithTimeline(shouldExpandTimeline, timelineStart, timelineEnd)) ;
                var apiPersonnel = departmentPersonnel?.Select(p => new ApiInternalPersonnelPerson(p)).ToList();
                var sectorDepartment = new SectorDepartment(departmentString, apiPersonnel);
                apiSector.SectorDepartments.Add(sectorDepartment);
            }
            return apiSector;
        }

        private async Task<List<string>> FindChildDepartments(string sectorString)
        {
            var sectors = new[] {
            new { Sector = "TPD PRD FE MMS", Departments = new List<string>{ "TPD PRD FE MMS MAT1",
                                                                             "TPD PRD FE MMS MAT2",
                                                                             "TPD PRD FE MMS MEC1",
                                                                             "TPD PRD FE MMS MEC2",
                                                                             "TPD PRD FE MMS MEC3",
                                                                             "TPD PRD FE MMS MEC4",
                                                                             "TPD PRD FE MMS STR1",
                                                                             "TPD PRD FE MMS STR2" } },
            new { Sector = "TPD PRD PMC PCA", Departments = new List<string>{ "TPD PRD PMC PCA PCA1",
                                                                             "TPD PRD PMC PCA PCA2",
                                                                             "TPD PRD PMC PCA PCA3",
                                                                             "TPD PRD PMC PCA PCA4",
                                                                             "TPD PRD PMC PCA PCA5",
                                                                             "TPD PRD PMC PCA PCA6",
                                                                             "TPD PRD PMC PCA PCA7" } }
            };
      
            var departmentStrings = new List<String>();
            departmentStrings.AddRange(sectors.First(s => s.Sector == sectorString).Departments);
                                              
            return departmentStrings;
        }
    }

}
