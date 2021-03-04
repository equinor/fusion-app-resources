using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Integration;
using Fusion.Resources.Domain;
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

                r.AnyOf(or => {
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
            var lineOrgProfile = JsonConvert.DeserializeAnonymousType(content, new {
                isResourceOwner = false,
                fullDepartment = string.Empty,
                manager = new
                {
                    fullDepartment = string.Empty
                }
            });


            var sector = ResolveSector(lineOrgProfile.fullDepartment);

            // Resolve departments with responsibility
            var isDepartmentManager = lineOrgProfile.isResourceOwner && lineOrgProfile.fullDepartment != lineOrgProfile.manager?.fullDepartment;
            isDepartmentManager |= IsDelegatedResourceOwner(user.AzureUniqueId, lineOrgProfile.fullDepartment);

            var departmentsWithResponsibility = new List<string>();

            // Add the current department if the user is resource owner in the department.
            if (isDepartmentManager)
                departmentsWithResponsibility.Add(lineOrgProfile.fullDepartment);
            
            // Add all departments the user has been delegated responsibility for.
            departmentsWithResponsibility.AddRange(ResolveDepartmentsWithResponsibility(user.AzureUniqueId));


            // Get sectors the user have responsibility in, to find all relevant departments
            var relevantSectors = departmentsWithResponsibility
                .Select(d => ResolveSector(d))
                .Where(s => !string.IsNullOrEmpty(s))
                .Cast<string>()
                .Distinct()
                .ToList();

            // If the sector does not exist, the person might be higher up. 
            if (sector is null && isDepartmentManager)
            {
                relevantSectors.AddRange(ResolveDownstreamSectors(lineOrgProfile.fullDepartment));
            }

            var relevantDepartments = relevantSectors
                .SelectMany(s => ResolveSectorDepartments(s))
                .ToList();


            var respObject = new ApiResourceOwnerProfile(lineOrgProfile.fullDepartment)
            {
                IsResourceOwner = isDepartmentManager,
                Sector = sector,
                ResponsibilityInDepartments = departmentsWithResponsibility,
                RelevantDepartments = relevantDepartments,
                RelevantSectors = relevantSectors
            };

            return respObject;
        }


        private static Dictionary<string, string> departmentSectors = null!;
        private string? ResolveSector(string department)
        {
            LoadSectorInfo();

            return departmentSectors.TryGetValue(department.ToUpper(), out string? depSector) ? depSector : null;
        }
        private IEnumerable<string> ResolveSectorDepartments(string sector)
        {
            LoadSectorInfo();

            return departmentSectors
                .Where(kv => kv.Value == sector.ToUpper())
                .Select(kv => kv.Key)
                .ToList();
        }

        private IEnumerable<string> ResolveDownstreamSectors(string department)
        {
            LoadSectorInfo();

            return departmentSectors.Values.Distinct().Where(s => s.StartsWith(department, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadSectorInfo()
        {
            if (departmentSectors is null)
            {
                departmentSectors = FetchSectors();
            }
        }

        public static Dictionary<string, string> FetchSectors()
        {
            var departmentSectors = new Dictionary<string, string>();

            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Fusion.Resources.Api.Controllers.Person.departmentSectors.json"))
            using (var r = new StreamReader(s!))
            {
                var json = r.ReadToEnd();

                var sectorInfo = JsonConvert.DeserializeAnonymousType(json, new[] { new { sector = string.Empty, departments = Array.Empty<string>() } });


                foreach (var sector in sectorInfo)
                {
                    sector.departments.ToList().ForEach(d => departmentSectors[d] = sector.sector);
                    departmentSectors[sector.sector] = sector.sector;
                }
            }
            return departmentSectors;
        }

        private IEnumerable<string> ResolveDepartmentsWithResponsibility(Guid? azureUniqueId)
        {
            if (azureUniqueId is null)
                yield break;

            foreach (var dep in delegatedDepartmentLeaders.Where(d => d.Item1 == azureUniqueId.Value))
                yield return dep.Item2;
        }

        private bool IsDelegatedResourceOwner(Guid? azureUniqueId, string fullDepartment)
        {
            if (azureUniqueId is null)
                return false;

            return delegatedDepartmentLeaders.Any(kv => kv.Item1 == azureUniqueId && string.Equals(kv.Item2, fullDepartment, StringComparison.OrdinalIgnoreCase));
        }

        private static List<ValueTuple<Guid, string>> delegatedDepartmentLeaders = new()
        {
            ( new Guid("20621fbc-dc4e-4958-95c9-2ac56e166973"), "TPD PRD PMC PCA PCA7" )
        };
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