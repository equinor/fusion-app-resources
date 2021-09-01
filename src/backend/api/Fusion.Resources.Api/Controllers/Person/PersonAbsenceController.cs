using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Fusion.Resources.Domain.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class PersonAbsenceController : ResourceControllerBase
    {
        [HttpGet("/persons/{personId}/absence")]
        public async Task<ActionResult<ApiCollection<ApiPersonAbsence>>> GetPersonAbsence(
            [FromRoute] string personId)
        {
            var id = new PersonId(personId);

            var profile = await DispatchAsync(new GetPersonProfile(id));
            if (profile is null)
                return ApiErrors.NotFound($"Person with id '{personId}' could not be found.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen()
                    .CurrentUserIs(profile.Identifier)
                    .FullControl()
                    .FullControlInternal();

                r.AnyOf(or =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                });
                r.LimitedAccessWhen(x =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                        x.BeResourceOwner(new DepartmentPath(profile.FullDepartment).GoToLevel(2), includeParents: false, includeDescendants: true);
                });
            });
            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var personAbsence = await DispatchAsync(new GetPersonAbsence(id));

            var returnItems = personAbsence.Select(p => authResult.LimitedAuth 
                ? ApiPersonAbsence.CreateWithoutConfidentialTaskInfo(p)
                : ApiPersonAbsence.CreateWithConfidentialTaskInfo(p)
            );

            var collection = new ApiCollection<ApiPersonAbsence>(returnItems);
            return collection;
        }

        [HttpGet("/persons/{personId}/absence/{absenceId}")]
        public async Task<ActionResult<ApiPersonAbsence>> GetPersonAbsence([FromRoute] string personId,
            Guid absenceId)
        {
            var id = new PersonId(personId);

            var profile = await DispatchAsync(new GetPersonProfile(id));
            if (profile is null)
                return ApiErrors.NotFound($"Person with id '{personId}' could not be found.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                });
                r.LimitedAccessWhen(x =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                        x.BeResourceOwner(new DepartmentPath(profile.FullDepartment).GoToLevel(2), includeParents: false, includeDescendants: true);
                });
            });
            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var personAbsence = await DispatchAsync(new GetPersonAbsenceItem(id, absenceId));

            if (personAbsence == null)
                return FusionApiError.NotFound(absenceId, "Could not locate absence registration");

            return authResult.LimitedAuth
                ? ApiPersonAbsence.CreateWithoutConfidentialTaskInfo(personAbsence)
                : ApiPersonAbsence.CreateWithConfidentialTaskInfo(personAbsence);
        }

        [HttpPost("/persons/{personId}/absence")]
        public async Task<ActionResult<ApiPersonAbsence>> CreatePersonAbsence([FromRoute] string personId,
            [FromBody] CreatePersonAbsenceRequest request)
        {
            var id = new PersonId(personId);

            var profile = await DispatchAsync(new GetPersonProfile(id));
            if (profile is null)
                return ApiErrors.NotFound($"Person with id '{personId}' could not be found.");


            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var createCommand = new CreatePersonAbsence(id);
            request.LoadCommand(createCommand);

            try
            {
                using (var scope = await BeginTransactionAsync())
                {
                    var newAbsence = await DispatchAsync(createCommand);
                    await scope.CommitAsync();

                    var item = ApiPersonAbsence.CreateWithConfidentialTaskInfo(newAbsence);
                    return Created($"/persons/{personId}/absence/{item.Id}", item);
                }
            }
            catch (InvalidOperationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
        }

        [HttpPut("/persons/{personId}/absence/{absenceId}")]
        public async Task<ActionResult<ApiPersonAbsence>> UpdatePersonAbsence([FromRoute] string personId,
            Guid absenceId, [FromBody] UpdatePersonAbsenceRequest request)
        {
            var id = new PersonId(personId);
            var profile = await DispatchAsync(new GetPersonProfile(id));
            if (profile is null)
                return ApiErrors.NotFound($"Person with id '{personId}' could not be found.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var updateCommand = new UpdatePersonAbsence(id, absenceId);
            request.LoadCommand(updateCommand);

            using (var scope = await BeginTransactionAsync())
            {
                var updatedAbsence = await DispatchAsync(updateCommand);

                await scope.CommitAsync();

                var item = ApiPersonAbsence.CreateWithConfidentialTaskInfo(updatedAbsence);
                return item;
            }
        }


        [HttpDelete("/persons/{personId}/absence/{absenceId}")]
        public async Task<ActionResult> DeletePersonAbsence([FromRoute] string personId, Guid absenceId)
        {
            var id = new PersonId(personId);
            var profile = await DispatchAsync(new GetPersonProfile(id));
            if (profile is null)
                return ApiErrors.NotFound($"Person with id '{personId}' could not be found.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion


            await DispatchAsync(new DeletePersonAbsence(id, absenceId));

            return NoContent();
        }

        [HttpOptions("/persons/{personId}/absence")]
        public async Task<ActionResult> GetOptionsForPerson(string personId)
        {
            var allowedVerbs = new List<string>();

            var id = new PersonId(personId);
            var profile = await DispatchAsync(new GetPersonProfile(id));
            if (profile is null)
                return ApiErrors.NotFound($"Person with id '{personId}' could not be found.");
            
            var getAuthResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).GoToLevel(2), includeParents: false, includeDescendants: true);
                });
            });
            if (getAuthResult.Success) allowedVerbs.Add("GET");

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                });
            });

            if (authResult.Success) allowedVerbs.Add("GET", "POST");

            Response.Headers["Allow"] = string.Join(',', allowedVerbs.Distinct());
            return NoContent();
        }

        [HttpOptions("/persons/{personId}/absence/{absenceId}")]
        public async Task<ActionResult> GetOptions(string personId, Guid absenceId)
        {
            var allowedVerbs = new List<string>();

            var id = new PersonId(personId);
            var profile = await DispatchAsync(new GetPersonProfile(id));
            if (profile is null)
                return ApiErrors.NotFound($"Person with id '{personId}' could not be found.");

            var getAuthResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).GoToLevel(2), includeParents: false, includeDescendants: true);
                });
            });
            if (getAuthResult.Success) allowedVerbs.Add("GET");

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();
                r.AlwaysAccessWhen().FullControlInternal();

                r.AnyOf(or =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                });
            });

            if (authResult.Success) allowedVerbs.Add("PUT", "DELETE");

            Response.Headers["Allow"] = string.Join(',', allowedVerbs.Distinct());
            return NoContent();
        }
    }
}