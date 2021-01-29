using System;
using System.Linq;
using System.Threading.Tasks;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Resources.Api.Authorization;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class ResponsibilityMatrixController : ResourceControllerBase
    {
        [HttpGet("/internal-resources/responsibility-matrix")]
        public async Task<ActionResult<ApiCollection<ApiResponsibilityMatrix>>> GetResponsibilityMatrix()
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ScopeAccess(ScopeAccess.ManageMatrices);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var responsibilityMatrix = await DispatchAsync(new GetResponsibilityMatrices());

            var returnItems = responsibilityMatrix.Select(p => new ApiResponsibilityMatrix(p));

            var collection = new ApiCollection<ApiResponsibilityMatrix>(returnItems);
            return collection;
        }

        [HttpGet("/internal-resources/responsibility-matrix/{matrixId}")]
        public async Task<ActionResult<ApiResponsibilityMatrix>> GetResponsibilityMatrix(Guid matrixId)
        {
            var responsibilityMatrix = await DispatchAsync(new GetResponsibilityMatrixItem(matrixId));

            if (responsibilityMatrix == null)
                return FusionApiError.NotFound(matrixId, "Could not locate responsibility matrix");

            var returnItem = new ApiResponsibilityMatrix(responsibilityMatrix);
            return returnItem;
        }

        [HttpPost("/internal-resources/responsibility-matrix")]
        public async Task<ActionResult<ApiResponsibilityMatrix>> CreateResponsibilityMatrix([FromBody] CreateResponsibilityMatrixRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ScopeAccess(ScopeAccess.ManageMatrices);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var createCommand = new CreateResponsibilityMatrix();
            request.LoadCommand(createCommand);

            try
            {
                using (var scope = await BeginTransactionAsync())
                {
                    var newAbsence = await DispatchAsync(createCommand);
                    await scope.CommitAsync();

                    var item = new ApiResponsibilityMatrix(newAbsence);
                    return Created($"/internal-resources/responsibility-matrix/{item.Id}", item);
                }
            }
            catch (InvalidOperationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
        }

        [HttpPut("/internal-resources/responsibility-matrix/{matrixId}")]
        public async Task<ActionResult<ApiResponsibilityMatrix>> UpdateResponsibilityMatrix(Guid matrixId, [FromBody] UpdateResponsibilityMatrixRequest request)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ScopeAccess(ScopeAccess.ManageMatrices);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            var updateCommand = new UpdateResponsibilityMatrix(matrixId);
            request.LoadCommand(updateCommand);

            using (var scope = await BeginTransactionAsync())
            {
                var updatedAbsence = await DispatchAsync(updateCommand);

                await scope.CommitAsync();

                var item = new ApiResponsibilityMatrix(updatedAbsence);
                return item;
            }
        }


        [HttpDelete("/internal-resources/responsibility-matrix/{matrixId}")]
        public async Task<ActionResult> DeleteResponsibilityMatrix(Guid matrixId)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ScopeAccess(ScopeAccess.ManageMatrices);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            await DispatchAsync(new DeleteResponsibilityMatrix(matrixId));

            return NoContent();
        }
    }
}