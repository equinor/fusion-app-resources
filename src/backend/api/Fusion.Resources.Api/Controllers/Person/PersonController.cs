using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Integration;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Authorization;
using Fusion.Resources.Domain;
using Fusion.Integration.Profile;
using Fusion.Integration.LineOrg;
using Fusion.AspNetCore.OData;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Fusion.Resources.Api.Controllers.Departments;

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
    [ApiVersion("1.0")]
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
            if (resourceOwnerProfile is null) return ApiErrors.NotFound($"No profile found for user {personId}.");

            return new ApiResourceOwnerProfile(resourceOwnerProfile);
        }


        [HttpGet("/persons/me/resources/relevant-departments")]
        [HttpGet("/persons/{personId}/resources/relevant-departments")]
        public async Task<ActionResult<OrgApiPagedCollection<ApiRelevantDepartmentProfile>>> GetRelevantDepartments(string? personId, [FromQuery] ODataQueryParams query)
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


            // Remap api model to query model
            query.MapFilterFields<ApiRelevantDepartmentProfile>(f => f.MapToModel<ApiRelevantDepartmentProfile>()
                .MapField(a => a.fullDepartment, q => q.fullDepartment)
                .MapField(a => a.sapId, q => q.sapId)
                .MapField(a => a.parentSapId, q => q.parentSapId)
                .MapField(a => a.department, q => q.department)
                .MapField(a => a.name, q => q.name)
                .MapField(a => a.shortName, q => q.shortName)

            );


            var resourceOwnerProfile = await DispatchAsync(new GetRelevantOrgUnits(personId, query));
            if (resourceOwnerProfile is null) return ApiErrors.NotFound($"No profile found for user {personId}.");

            var collection = resourceOwnerProfile.Select(x => new ApiRelevantDepartmentProfile(x)).ToList();
            var top = query.Top;

    
            //var returnItems = new ApiCollection<ApiRelevantDepartmentProfile>(collection) { TotalCount = collection.Count() };
            var returnItems = new OrgApiPagedCollection<ApiRelevantDepartmentProfile>(collection, collection.Count()).SetPagingUrls(query, Request); ;
            return returnItems;
        }

        [HttpGet("/persons/{personId}/resources/notes")]
        public async Task<ActionResult<List<ApiPersonNote>>> GetPersonNotes(string personId)
        {
            var user = await EnsureUserAsync(personId);
            if (user.error is not null)
                return user.error;

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    or.BeResourceOwner(user.fullDepartment);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(user.fullDepartment), AccessRoles.ResourceOwner);
                });

                // Limited access to other resource owners, only return shared notes.
                // Give access to all resource owners that share the same L3.
                r.LimitedAccessWhen(or => or.BeResourceOwner(new DepartmentPath(user.fullDepartment).GoToLevel(3), includeParents: true, includeDescendants: true));
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var notes = await DispatchAsync(new GetPersonNotes(user.azureId));

            // If limited auth, only return shared notes.
            notes = authResult.FilterWhenLimited(notes, n => n.IsShared);

            return notes.Select(n => new ApiPersonNote(n)).ToList();
        }

        [HttpPut("/persons/{personId}/resources/notes/{noteId}")]
        public async Task<ActionResult<ApiPersonNote>> UpdatePersonalNote(string personId, Guid noteId, [FromBody] PersonNotesRequest request)
        {

            var user = await EnsureUserAsync(personId);
            if (user.error is not null)
                return user.error;

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    or.BeResourceOwner(user.fullDepartment);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(user.fullDepartment), AccessRoles.ResourceOwner);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            var notes = await DispatchAsync(new GetPersonNotes(user.azureId));
            if (!notes.Any(n => n.Id == noteId))
                return ApiErrors.NotFound("Could not locate note for user");

            var updatedNote = await DispatchAsync(Domain.Commands.CreateOrUpdatePersonNote.Update(noteId, request.Content, user.azureId)
                .WithTitle(request.Title)
                .SetIsShared(request.IsShared));

            return new ApiPersonNote(updatedNote);
        }

        [HttpPost("/persons/{personId}/resources/notes")]
        public async Task<ActionResult<ApiPersonNote>> CreateNewPersonalNote(string personId, [FromBody] PersonNotesRequest request)
        {
            var user = await EnsureUserAsync(personId);
            if (user.error is not null)
                return user.error;

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen()
                    .FullControl()
                    .FullControlInternal();

                r.AnyOf(or =>
                {
                    or.BeResourceOwner(user.fullDepartment);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(user.fullDepartment), AccessRoles.ResourceOwner);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var newNote = await DispatchAsync(Domain.Commands.CreateOrUpdatePersonNote.CreateNew(request.Content, user.azureId)
                .WithTitle(request.Title)
                .SetIsShared(request.IsShared));

            return new ApiPersonNote(newNote);
        }

        [HttpDelete("/persons/{personId}/resources/notes/{noteId}")]
        public async Task<ActionResult> DeletePersonalNote(string personId, Guid noteId)
        {

            var user = await EnsureUserAsync(personId);
            if (user.error is not null)
                return user.error!;

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    or.BeResourceOwner(user.fullDepartment);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(user.fullDepartment), AccessRoles.ResourceOwner);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion



            var notes = await DispatchAsync(new GetPersonNotes(user.azureId));
            if (!notes.Any(n => n.Id == noteId))
                return ApiErrors.NotFound("Could not locate note for user");

            await DispatchAsync(new Domain.Commands.DeletePersonNote(noteId, user.azureId));

            return NoContent();
        }

        [HttpOptions("/persons/{personId}/resources/notes")]
        public async Task<ActionResult> GetPersonNoteOptions(string personId)
        {
            var user = await EnsureUserAsync(personId);

            var allowedVerbs = new List<string>();

            var getResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    or.BeResourceOwner(user.fullDepartment);
                });

                // Limited access to other resource owners, only return shared notes.
                // Give access to all resource owners that share the same L3.
                r.LimitedAccessWhen(or => or.BeResourceOwner(new DepartmentPath(user.fullDepartment).GoToLevel(3), includeParents: true, includeDescendants: true));
            });

            if (getResult.Success) allowedVerbs.Add("GET");
            if (getResult.Success && !getResult.LimitedAuth) allowedVerbs.Add("POST", "PUT", "DELETE");

            Response.Headers["Allow"] = string.Join(',', allowedVerbs);
            return NoContent();
        }

        /// <summary>
        /// Access: 
        ///     The endpoint should be internal by default, as we are not returning any other than internal data.
        ///     External accounts should only have access when they have been assigned affiliate role in access it. This excludes accounts that have been added to the domain through ex. SharePoint file share.
        /// </summary>
        /// <param name="personId">Azure unique id or upn/mail</param>
        /// <returns></returns>
        [HttpGet("/persons/{personId}/resources/allocation-request-status")]
        public async Task<ActionResult<ApiPersonAllocationRequestStatus>> GetPersonRequestAllocationStatus(string personId)
        {
            var user = await EnsureUserAsync(personId);
            if (user.error is not null)
                return user.error!;

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    or.BeAccountType(FusionAccountType.Employee, FusionAccountType.Consultant, FusionAccountType.Application);
                });
                r.LimitedAccessWhen(or => or.BeAccountType(FusionAccountType.External));
            });


            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            // Should check if the user has affiliate access to fusion here.
            if (authResult.LimitedAuth)
                return FusionApiError.Forbidden("External accounts does not have access");


            var autoApproval = await DispatchAsync(new Domain.Queries.GetPersonAutoApprovalStatus(user.azureId));
            var manager = await DispatchAsync(new Domain.Queries.GetResourceOwner(user.azureId));

            return new ApiPersonAllocationRequestStatus
            {
                AutoApproval = autoApproval.GetValueOrDefault(false),
                Manager = manager is not null ? new ApiPerson(manager) : null
            };
        }


        private async Task<(Guid azureId, string fullDepartment, ActionResult? error)> EnsureUserAsync(string personId)
        {
            var user = await profileResolver.ResolvePersonBasicProfileAsync(personId);
            if (user is null)
                return (Guid.Empty, string.Empty, ApiErrors.NotFound("Could not locate user"));
            if (user.AzureUniqueId is null)
                return (Guid.Empty, string.Empty, ApiErrors.InvalidInput("Could not locate any unique id for the user. User must exist in ad."));

            return (user.AzureUniqueId.Value, user.FullDepartment ?? string.Empty, null);
        }
    }

    public class OrgApiPagedCollection<T>
    {
        public OrgApiPagedCollection()
        {
            Value = Array.Empty<T>();
            TotalCount = 0;
            Count = 0;
        }
        public OrgApiPagedCollection(IEnumerable<T> items, int totalCount)
        {
            Value = items;
            TotalCount = totalCount;
            Count = items.Count();
        }

        /// <summary>
        /// Total count without paging applied. This count indicates how many results exists with just using the filters.
        /// </summary>
        public int TotalCount { get; set; }
        public int Count { get; set; }

        /// <summary>
        /// Always show the nextpage, as that is an indication that there are no more pages.
        /// </summary>
        [JsonProperty(PropertyName = "@nextPage", NullValueHandling = NullValueHandling.Include)]
        public string? NextPage { get; set; }

        /// <summary>
        /// Convenience only, hide if not applicable.
        /// </summary>
        [JsonProperty(PropertyName = "@prevPage", NullValueHandling = NullValueHandling.Ignore)]
        public string? PrevPage { get; set; }

        [JsonProperty(PropertyName = "value", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<T> Value { get; set; }


        public OrgApiPagedCollection<T> SetPagingUrls(ODataQueryParams queryParams, HttpRequest request)
        {
            var currentUrl = $"{request.Path}{request.QueryString}";

            var currentSkip = queryParams.Skip.GetValueOrDefault(0);
            var pageSize = queryParams.Top.GetValueOrDefault(Count);

            // Is there a next page?
            var nextSkip = currentSkip + pageSize;
            var prevSkip = Math.Max(currentSkip - pageSize, 0);

            currentUrl = EnsureSkip(currentUrl);

            if (nextSkip < TotalCount)
            {
                var nextUrl = Regex.Replace(currentUrl, @"\$skip=\d+", $"$skip={nextSkip}");
                NextPage = nextUrl;
            }

            if (currentSkip > 0)
            {
                var prevUrl = Regex.Replace(currentUrl, @"\$skip=\d+", $"$skip={prevSkip}");
                PrevPage = prevUrl;
            }

            return this;
        }

        private static string EnsureSkip(string currentUrl)
        {
            // NextPage/PrevPage is dependant on a skip argument to be able to create urls.
            if (Regex.IsMatch(currentUrl, @"\$skip=\d+"))
            {
                return currentUrl;
            }

            var arg = currentUrl.Contains("?") ? "&" : "?";
            currentUrl = currentUrl.TrimEnd('/') + $"{arg}$skip=0";
            return currentUrl;
        }
    }
}