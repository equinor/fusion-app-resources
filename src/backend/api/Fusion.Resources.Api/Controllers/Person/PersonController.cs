using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Integration;
using Fusion.Resources.Api.Controllers.Departments;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{
    /// <summary>
    /// 
    /// NOTE: Prototype endpoint with lot of hard coded data as we are waiting for proper data set.
    /// 
    /// This should be refactored when dependent datasets like sector/department overview has been set.
    /// 
    /// </summary>
    [ApiVersion("1.0-preview")]
    [Authorize]
    [ApiController]
    public class PersonController : ResourceControllerBase
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IFusionProfileResolver profileResolver;

        public PersonController(IHttpClientFactory httpClientFactory, IFusionProfileResolver profileResolver)
        {
            this.httpClientFactory = httpClientFactory;
            this.profileResolver = profileResolver;
        }

        [HttpGet("/persons/me/resources/profile")]
        [HttpGet("/persons/{personId}/resources/profile")]
        public async Task<ActionResult<ApiResourceOwnerProfile>> GetResourceProfile(string? personId)
        {

            if (string.IsNullOrEmpty(personId) || string.Equals(personId, "me", StringComparison.OrdinalIgnoreCase))
                personId = $"{User.GetAzureUniqueId()}";

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    or.CurrentUserIs(personId);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            var user = await profileResolver.ResolvePersonBasicProfileAsync(personId);

            if (user is null)
                return ApiErrors.NotFound($"Could not locate any usser with id '{personId}'");

            if (user.AzureUniqueId is null)
                return FusionApiError.InvalidOperation("InvalidUser", "The user does not have an azure unique id and cannot be used.");


            var client = httpClientFactory.CreateClient("lineorg");

            var resp = await client.GetAsync($"lineorg/persons/{user.AzureUniqueId}");

            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return ApiErrors.InvalidInput("Invalid account, user does not exist in the line.");

            if (!resp.IsSuccessStatusCode)
                return ApiErrors.FailedDependency("Lineorg", $"Could not resolve line info: {resp.StatusCode}");

            var content = await resp.Content.ReadAsStringAsync();
            var lineOrgProfile = JsonConvert.DeserializeAnonymousType(content, new
            {
                isResourceOwner = false,
                fullDepartment = string.Empty,
                manager = new
                {
                    fullDepartment = string.Empty
                }
            });

            var sector = await ResolveSector(lineOrgProfile.fullDepartment);

            // Resolve departments with responsibility
            var isDepartmentManager = lineOrgProfile.isResourceOwner && lineOrgProfile.fullDepartment != lineOrgProfile.manager?.fullDepartment;

            var departmentsWithResponsibility = new List<string>();

            // Add the current department if the user is resource owner in the department.
            if (isDepartmentManager)
                departmentsWithResponsibility.Add(lineOrgProfile.fullDepartment);

            // Add all departments the user has been delegated responsibility for.
            var delegatedResponsibilities = await DispatchAsync(new GetDelegatedDepartmentResponsibilty(user.AzureUniqueId));
            isDepartmentManager |= delegatedResponsibilities.Any(r => r.DepartmentId == lineOrgProfile.fullDepartment);
            departmentsWithResponsibility.AddRange(delegatedResponsibilities.Select(r => r.DepartmentId));

            // Get sectors the user have responsibility in, to find all relevant departments
            var relevantSectors = new HashSet<string>();
            foreach (var department in departmentsWithResponsibility)
            {
                var resolvedSector = await ResolveSector(department);
                if (resolvedSector != null && !relevantSectors.Contains(resolvedSector))
                {
                    relevantSectors.Add(resolvedSector);
                }
            }

            // If the sector does not exist, the person might be higher up. 
            if (sector is null && isDepartmentManager)
            {
                var downstreamSectors = await ResolveDownstreamSectors(lineOrgProfile.fullDepartment);
                foreach (var department in downstreamSectors)
                {
                    var resolvedSector = await ResolveSector(department);
                    if (resolvedSector != null && !relevantSectors.Contains(resolvedSector))
                    {
                        relevantSectors.Add(resolvedSector);
                    }
                }
            }

            var relevantDepartments = new List<string>();
            foreach (var relevantSector in relevantSectors)
            {
                relevantDepartments.AddRange(await ResolveSectorDepartments(relevantSector));
            }

            var respObject = new ApiResourceOwnerProfile(lineOrgProfile.fullDepartment)
            {
                IsResourceOwner = isDepartmentManager,
                Sector = sector,
                ResponsibilityInDepartments = departmentsWithResponsibility,
                RelevantDepartments = relevantDepartments,
                RelevantSectors = relevantSectors.ToList()
            };

            return respObject;
        }

        [HttpGet("/persons/resource-owners")]
        public async Task<ActionResult<List<ApiDepartment>>> GetResourceOwners([FromQuery(Name = "q")] string query)
        {
            var request = new SearchResourceOwners(query);
            var result = await DispatchAsync(request);

            return Ok(result.Select(x => new ApiDepartment(x)));
        }

        private async Task<string?> ResolveSector(string department)
        {
            var request = new GetDepartmentSector(department);
            return await DispatchAsync(request);
        }
        private async Task<IEnumerable<string>> ResolveSectorDepartments(string sector)
        {
            var departments = await DispatchAsync(new GetDepartments().InSector(sector));
            return departments
                .Select(dpt => dpt.DepartmentId);
        }

        private async Task<IEnumerable<string>> ResolveDownstreamSectors(string department)
        {
            var departments = await DispatchAsync(new GetDepartments().StartsWith(department));
            return departments
                .Select(dpt => dpt.SectorId!).Distinct();
        }
    }

    public class ApiResourceOwnerProfile
    {
        public ApiResourceOwnerProfile(string fullDepartmentString)
        {
            FullDepartment = fullDepartmentString;
        }

        public string FullDepartment { get; set; }
        public string? Sector { get; set; }


        public bool IsResourceOwner { get; set; }

        public List<string> ResponsibilityInDepartments { get; set; } = new();

        public List<string> RelevantDepartments { get; set; } = new();
        public List<string> RelevantSectors { get; set; } = new();
    }


}