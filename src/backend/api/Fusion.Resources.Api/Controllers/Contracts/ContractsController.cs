using Fusion.Integration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class ContractsController : ResourceControllerBase
    {

        [Obsolete]
        [HttpGet("/projects/{projectIdentifier}/contracts")]
        public ActionResult GetProjectAllocatedContract([FromRoute]PathProjectIdentifier projectIdentifier)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpGet("/projects/{projectIdentifier}/contracts/{contractId}")]
        public ActionResult GetProjectContract([FromRoute]PathProjectIdentifier projectIdentifier, Guid contractId)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpGet("/projects/{projectIdentifier}/available-contracts")]
        public ActionResult GetProjectAvailableContracts(
            [FromRoute]PathProjectIdentifier projectIdentifier,
            [FromServices] IHttpClientFactory httpClientFactory,
            [FromServices] IFusionContextResolver contextResolver)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPost("/projects/{projectIdentifier}/contracts")]
        public ActionResult AllocateProjectContract([FromRoute]PathProjectIdentifier projectIdentifier, [FromBody] object request)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}")]
        public ActionResult UpdateProjectContract([FromRoute]PathProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] object request)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/external-company-representative")]
        public ActionResult EnsureContractExternalCompanyRep([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] object request)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/external-contract-responsible")]
        public ActionResult EnsureContractExternalContractResp([FromRoute]PathProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] object request)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        #region Role delegation

        [Obsolete]
        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/delegated-roles")]
        public ActionResult GetContractDelegatedRoles([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/delegated-roles")]
        public ActionResult CreateContractDelegatedRole([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] object request)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }
        
        [Obsolete]
        [HttpDelete("/projects/{projectIdentifier}/contracts/{contractIdentifier}/delegated-roles/{roleId}")]
        public ActionResult DeleteContractDelegatedRole([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid roleId)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPatch("/projects/{projectIdentifier}/contracts/{contractIdentifier}/delegated-roles/{roleId}")]
        public ActionResult UpdateContractDelegatedRole([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid roleId, [FromBody] object request)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/delegated-roles")]
        public ActionResult CheckContractDelegationAccess([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromQuery] string classification)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        #endregion
    }
}
