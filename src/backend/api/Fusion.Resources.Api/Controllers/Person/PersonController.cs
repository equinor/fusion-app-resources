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
using Microsoft.VisualBasic;
using NodaTime.TimeZones;

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
            if (resourceOwnerProfile is null)
                return ApiErrors.NotFound($"No profile found for user {personId}.");

            return new ApiResourceOwnerProfile(resourceOwnerProfile);
        }

        [HttpGet("/persons/me/resources/relevant-departments")]
        [HttpGet("/persons/{personId}/resources/relevant-departments")]
        public async Task<ActionResult<ApiCollection<ApiRelevantOrgUnit>>> GetRelevantDepartments(string? personId, [FromQuery] ODataQueryParams query)
        {
            if (string.IsNullOrEmpty(personId) || string.Equals(personId, "me", StringComparison.OrdinalIgnoreCase))
                personId = $"{User.GetAzureUniqueId()}";

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl().FullControlInternal();
                r.AnyOf(or =>
                                {
                                    or.CurrentUserIs(personId);
                                    or.BeTrustedApplication();
                                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            var relevantOrgUnits = await DispatchAsync(new GetRelevantOrgUnits(personId, query));
            if (relevantOrgUnits is null)
                return ApiErrors.NotFound($"No relevant OrgUnits found for user {personId}.");

            var collection = new ApiCollection<ApiRelevantOrgUnit>(relevantOrgUnits.Select(x => new ApiRelevantOrgUnit(x))) { TotalCount = relevantOrgUnits.TotalCount };
            return collection;

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
                    or.BeResourceOwnerForDepartment(user.fullDepartment);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(user.fullDepartment), AccessRoles.ResourceOwner);
                });

                // Limited access to other resource owners, only return shared notes.
                // Give access to all resource owners that share the same L3.
                r.LimitedAccessWhen(or => or.BeResourceOwnerForDepartment(new DepartmentPath(user.fullDepartment).GoToLevel(3), includeParents: true, includeDescendants: true));
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
                    or.BeResourceOwnerForDepartment(user.fullDepartment);
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
                    or.BeResourceOwnerForDepartment(user.fullDepartment);
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
                    or.BeResourceOwnerForDepartment(user.fullDepartment);
                    or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(user.fullDepartment), AccessRoles.ResourceOwner);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion



            var notes = await DispatchAsync(new GetPersonNotes(user.azureId));
            if (!notes.Any(n => n.Id == noteId))
                return ApiErrors.NotFound("Could not locate note for user");

            await DispatchCommandAsync(new Domain.Commands.DeletePersonNote(noteId, user.azureId));

            return NoContent();
        }

        [EmulatedUserSupport]
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
                    or.BeResourceOwnerForDepartment(user.fullDepartment);
                });

                // Limited access to other resource owners, only return shared notes.
                // Give access to all resource owners that share the same L3.
                r.LimitedAccessWhen(or => or.BeResourceOwnerForDepartment(new DepartmentPath(user.fullDepartment).GoToLevel(3), includeParents: true, includeDescendants: true));
            });

            if (getResult.Success)
                allowedVerbs.Add("GET");
            if (getResult.Success && !getResult.LimitedAuth)
                allowedVerbs.Add("POST", "PUT", "DELETE");

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
}