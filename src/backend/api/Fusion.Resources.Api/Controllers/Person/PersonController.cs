using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Integration;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Authorization;

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
            var postResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    or.BeResourceOwner(user.fullDepartment);
                });
            });
            if (postResult.Success) allowedVerbs.Add("POST");

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

            Response.Headers["Allow"] = string.Join(',', allowedVerbs);
            return NoContent();
        }
        [HttpOptions("/persons/{personId}/resources/notes/{noteId}")]
        public async Task<ActionResult> GetPersonNoteOptions(string personId, Guid noteId)
        {
            var allowedVerbs = new List<string>();

            var (azureId, fullDepartment, error) = await EnsureUserAsync(personId);
            if (error is not null)
                return error;

            #region Authorization

            var writeResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    or.BeResourceOwner(fullDepartment);
                });
            });
            if (writeResult.Success) allowedVerbs.Add("GET", "PUT", "DELETE");


            Response.Headers["Allow"] = string.Join(',', allowedVerbs);
            return NoContent();
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