using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Authorization;
using Fusion.Events;
using Fusion.Integration.LineOrg;
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
        private readonly IEventNotificationClient notificationClient;

        public PersonAbsenceController(IEventNotificationClient notificationClient)
        {
            this.notificationClient = notificationClient;
        }

        /// <summary>
        /// List all absence for a person; leave, vacation and additional tasks.
        /// </summary>
        /// <remarks>
        /// List all absence of the user.
        /// Response is tailored to user auth level, where properties and items are removed if user does not have access to these...
        /// 
        /// > To list only additional tasks marked as public, use 'additional-task' endpoint.
        /// </remarks>
        /// <param name="personId"></param>
        /// <returns></returns>
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
                    {
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(profile.FullDepartment), AccessRoles.ResourceOwner);
                    }
                });
                r.LimitedAccessWhen(x =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                        x.BeResourceOwner(new DepartmentPath(profile.FullDepartment).GoToLevel(2), includeParents: false, includeDescendants: true);
                });
            });

            // Check if user should get just the task details.
            var isAllowedOtherTasks = await Request.RequireAuthorizationAsync(r => r.AnyOf(o => o.BeEmployee()));

            if (authResult.Unauthorized && isAllowedOtherTasks.Unauthorized)
            {
                return authResult.CreateForbiddenResponse();
            }

            #endregion

            var personAbsence = await DispatchAsync(new GetPersonAbsence(id));


            // If the user is only authorized to view other tasks, filter the result and return it.
            if (authResult.Unauthorized && isAllowedOtherTasks.Success)
            {
                var otherTasks = personAbsence.Where(a => a.Type == QueryAbsenceType.OtherTasks && a.IsPrivate == false)
                    .Select(ApiPersonAbsence.CreateAdditionTask)
                    .ToList();

                return new ApiCollection<ApiPersonAbsence>(otherTasks);
            }

            // Return all absence
            var returnItems = personAbsence.Select(p => authResult.LimitedAuth
                ? ApiPersonAbsence.CreateWithoutConfidentialTaskInfo(p)
                : ApiPersonAbsence.CreateWithConfidentialTaskInfo(p)
            );

            var collection = new ApiCollection<ApiPersonAbsence>(returnItems);
            return collection;
        }

        /// <summary>
        /// List only additional tasks marked as public.
        /// </summary>
        /// <remarks>
        /// List all the additional tasks the person is assigned to, which is marked public. Will only list absence marked public, regardless if user is manager or not.
        /// A more "clean" implementation of listing public additional tasks than the clusterfk above...
        /// 
        /// > **Authorization**
        /// > - Be the persons resource owner
        /// > - Be employee
        /// 
        /// </remarks>
        /// <param name="personId"></param>
        /// <returns></returns>
        [HttpGet("/persons/{personId}/additional-tasks")]
        public async Task<ActionResult<ApiCollection<ApiPersonAdditionalTask>>> GetPersonAdditionalTasks([FromRoute] string personId)
        {
            var id = new PersonId(personId);

            var profile = await DispatchAsync(new GetPersonProfile(id));
            if (profile is null)
                return ApiErrors.NotFound($"Person with id '{personId}' could not be found.");

            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    // If the user is the manager - regardless if ext. hire or employee...
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                    {
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(profile.FullDepartment), AccessRoles.ResourceOwner);
                    }

                    or.BeEmployee();
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var personAbsence = await DispatchAsync(new GetPersonAbsence(id));

            var otherTasks = personAbsence.Where(a => a.Type == QueryAbsenceType.OtherTasks && a.IsPrivate == false)
                    .Select(t => new ApiPersonAdditionalTask(t))
                    .ToList();

            return new ApiCollection<ApiPersonAdditionalTask>(otherTasks);
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
                r.AlwaysAccessWhen()
                    .CurrentUserIs(profile.Identifier)
                    .FullControl()
                    .FullControlInternal();

                r.AnyOf(or =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                    {
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(profile.FullDepartment), AccessRoles.ResourceOwner);
                    }
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
                    {
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(profile.FullDepartment), AccessRoles.ResourceOwner);
                    }
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var createCommand = new CreatePersonAbsence(id);
            request.LoadCommand(createCommand);

            try
            {
                // Due to implementation details in event transaction, this must be called from the correct async scope. Cannot be called from other async method.
                await using var eventTransaction = await notificationClient.BeginTransactionAsync();
                await using (var scope = await BeginTransactionAsync())
                {
                    var newAbsence = await DispatchAsync(createCommand);
                    await scope.CommitAsync();
                    await eventTransaction.CommitAsync();

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
                    {
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(profile.FullDepartment), AccessRoles.ResourceOwner);
                    }
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var updateCommand = new UpdatePersonAbsence(id, absenceId);
            request.LoadCommand(updateCommand);

            await using var eventTransaction = await notificationClient.BeginTransactionAsync();
            await using (var scope = await BeginTransactionAsync())
            {
                var updatedAbsence = await DispatchAsync(updateCommand);

                await scope.CommitAsync();
                await eventTransaction.CommitAsync();

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
                    {
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(profile.FullDepartment), AccessRoles.ResourceOwner);
                    }
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
                r.AlwaysAccessWhen()
                    .CurrentUserIs(profile.Identifier)
                    .FullControl()
                    .FullControlInternal();

                r.AnyOf(or =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                    {
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).GoToLevel(2), includeParents: false, includeDescendants: true);
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(profile.FullDepartment), AccessRoles.ResourceOwner);
                    }
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
                    {
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(profile.FullDepartment), AccessRoles.ResourceOwner);
                    }
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
                r.AlwaysAccessWhen()
                    .CurrentUserIs(profile.Identifier)
                    .FullControl()
                    .FullControlInternal();

                r.AnyOf(or =>
                {
                    if (!String.IsNullOrEmpty(profile.FullDepartment))
                    {
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).GoToLevel(2), includeParents: false, includeDescendants: true);
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(profile.FullDepartment), AccessRoles.ResourceOwner);
                    }
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
                    {
                        or.BeResourceOwner(new DepartmentPath(profile.FullDepartment).Parent(), includeParents: false, includeDescendants: true);
                        or.HaveOrgUnitScopedRole(DepartmentId.FromFullPath(profile.FullDepartment), AccessRoles.ResourceOwner);
                    }
                });
            });

            if (authResult.Success) allowedVerbs.Add("PUT", "DELETE");

            Response.Headers["Allow"] = string.Join(',', allowedVerbs.Distinct());
            return NoContent();
        }
    }
}