using Fusion.ApiClients.Org;
using Fusion.AspNetCore.FluentAuthorization;
using Fusion.Resources.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers.Mpp
{
    [Authorize]
    [ApiController]
    public class MppController : ResourceControllerBase
    {

        [HttpDelete("/projects/{projectIdentifier}/contracts/{contractIdentifier}/mpp/positions/{positionId}")]
        public async Task<ActionResult> DeleteContractPosition([FromRoute] ProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid positionId)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.AnyExternalRole, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.AnyExternalRole, projectIdentifier, contractIdentifier);
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            try
            {
                await Commands.DispatchAsync(new Domain.Commands.DeleteContractPosition(projectIdentifier.ProjectId, contractIdentifier, positionId));
            }
            catch (InvalidOperationException ex)
            {
                return ApiErrors.InvalidOperation(ex);
            }
            catch (OrgApiError apiEx)
            {
                return ApiErrors.FailedFusionRequest(Fusion.Integration.FusionEndpoint.ProOrganisation, apiEx.Error?.Message ?? apiEx.Message);
            }

            return NoContent();
        }

        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/mpp/positions")]
        public async Task<ActionResult> CheckDeleteAccess([FromRoute] ProjectIdentifier projectIdentifier, Guid contractIdentifier)
        {
            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AlwaysAccessWhen().FullControl();

                r.AnyOf(or =>
                {
                    or.ContractAccess(ContractRole.AnyExternalRole, projectIdentifier, contractIdentifier);
                    or.DelegatedContractAccess(DelegatedContractRole.AnyExternalRole, projectIdentifier, contractIdentifier);
                });
            });

            if (authResult.Success)
                Response.Headers.Add("Allow", "DELETE");
            else
                Response.Headers.Add("Allow", "");

            return NoContent();
        }

    }
}
