using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Integration;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

            var resourceOwnerProfile = await DispatchAsync(new GetResourceOwnerProfile(personId));

            var respObject = new ApiResourceOwnerProfile(resourceOwnerProfile);
            return respObject;
        }
    }

    public class ApiResourceOwnerProfile
    {
        public ApiResourceOwnerProfile(QueryResourceOwnerProfile resourceOwnerProfile)
        {
            FullDepartment = resourceOwnerProfile.FullDepartment!;
            Sector = resourceOwnerProfile.Sector;
            IsResourceOwner = resourceOwnerProfile.IsDepartmentManager;
            ResponsibilityInDepartments = resourceOwnerProfile.DepartmentsWithResponsibility;
            RelevantSectors = resourceOwnerProfile.RelevantSectors;

            ChildDepartments = resourceOwnerProfile.ChildDepartments;
            SiblingDepartments = resourceOwnerProfile.SiblingDepartments;

            // Compile the relevant department list
            RelevantDepartments = ResponsibilityInDepartments
                .Union(resourceOwnerProfile.ChildDepartments ?? new ())
                .Union(resourceOwnerProfile.SiblingDepartments ?? new ())                
                .Distinct()
                .ToList();

            if (Sector is not null && !RelevantDepartments.Contains(Sector))
                RelevantDepartments.Add(Sector);
        }

        public string FullDepartment { get; set; }
        public string? Sector { get; set; }


        public bool IsResourceOwner { get; set; }

        public List<string> ResponsibilityInDepartments { get; set; } = new();

        public List<string> RelevantDepartments { get; set; } = new();
        public List<string> RelevantSectors { get; set; } = new();

        public List<string>? ChildDepartments { get; set; } 
        public List<string>? SiblingDepartments { get; set; }
    }


}