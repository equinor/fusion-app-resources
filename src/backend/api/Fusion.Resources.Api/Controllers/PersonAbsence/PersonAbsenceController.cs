using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
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
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or => { });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var id = new PersonId(personId);
            var personAbsence = await DispatchAsync(new GetPersonAbsence(id));

            var returnItems = personAbsence.Select(p => new ApiPersonAbsence(p));

            var collection = new ApiCollection<ApiPersonAbsence>(returnItems);
            return collection;
        }

        [HttpGet("/persons/{personId}/absence/{absenceId}")]
        public async Task<ActionResult<ApiPersonAbsence>> GetPersonAbsence([FromRoute] string personId,
            Guid absenceId)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or => { });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var id = new PersonId(personId);

            var personAbsence = await DispatchAsync(new GetPersonAbsenceItem(id, absenceId));

            if (personAbsence == null)
                return FusionApiError.NotFound(absenceId, "Could not locate absence registration");

            var returnItem = new ApiPersonAbsence(personAbsence);
            return returnItem;
        }

        [HttpPost("/persons/{personId}/absence")]
        public async Task<ActionResult<ApiPersonAbsence>> CreatePersonAbsence([FromRoute] string personId,
            [FromBody] CreatePersonAbsenceRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or => { });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var id = new PersonId(personId);
            var createCommand = new CreatePersonAbsence(id);
            request.LoadCommand(createCommand);

            try
            {
                using (var scope = await BeginTransactionAsync())
                {
                    var newAbsence = await DispatchAsync(createCommand);
                    await scope.CommitAsync();

                    var item = new ApiPersonAbsence(newAbsence);
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
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or => { });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var id = new PersonId(personId);
            var updateCommand = new UpdatePersonAbsence(id, absenceId);
            request.LoadCommand(updateCommand);

            using (var scope = await BeginTransactionAsync())
            {
                var updatedAbsence = await DispatchAsync(updateCommand);

                await scope.CommitAsync();

                var item = new ApiPersonAbsence(updatedAbsence);
                return item;
            }
        }


        [HttpDelete("/persons/{personId}/absence/{absenceId}")]
        public async Task<ActionResult> DeletePersonAbsence([FromRoute] string personId, Guid absenceId)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or => { });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var id = new PersonId(personId);

            await DispatchAsync(new DeletePersonAbsence(id, absenceId));

            return NoContent();
        }
    }
}