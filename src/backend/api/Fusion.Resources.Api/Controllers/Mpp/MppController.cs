using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.AspNetCore.Http;

namespace Fusion.Resources.Api.Controllers.Mpp
{
    [Authorize]
    [ApiController]
    public class MppController : ResourceControllerBase
    {
        [Obsolete]
        [HttpDelete("/projects/{projectIdentifier}/contracts/{contractIdentifier}/mpp/positions/{positionId}")]
        public ActionResult DeleteContractPosition([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid positionId)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/mpp/positions")]
        public ActionResult CheckDeleteAccess([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }
    }
}
