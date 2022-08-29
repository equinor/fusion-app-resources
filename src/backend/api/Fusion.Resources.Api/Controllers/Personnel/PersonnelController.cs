using Fusion.AspNetCore.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Fusion.Resources.Api.Controllers
{

    [Authorize]
    [ApiController]
    public class PersonnelController : ResourceControllerBase
    {
        [Obsolete]
        [HttpGet("resources/personnel")]
        public ActionResult GetPersonnel([FromQuery] ODataQueryParams query)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel")]
        public ActionResult GetContractPersonnel([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromQuery] ODataQueryParams query)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{personIdentifier}")]
        public ActionResult GetContractPersonnel([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, string personIdentifier)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPost("resources/personnel/{personIdentifier}/refresh")]
        public ActionResult RefreshPersonnel(string personIdentifier)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel")]
        public ActionResult CreateContractPersonnel([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] object request)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel-collection")]
        public ActionResult CreateContractPersonnelBatch([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] IEnumerable<object> requests)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/refresh")]
        public ActionResult RefreshContractPersonnel([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{personIdentifier}")]
        public ActionResult UpdateContractPersonnel([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, string personIdentifier, [FromBody] object request)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpDelete("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{personIdentifier}")]
        public ActionResult DeleteContractPersonnel([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, string personIdentifier)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/preferred-contact")]
        public ActionResult UpdatePersonnelPreferredContactMails([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] object request)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/preferred-contact")]
        public ActionResult CheckContractorMailValid([FromQuery] string mail)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{personIdentifier}/replace")]
        public ActionResult CheckReplaceContractPersonnelAccess([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, string personIdentifier)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/personnel/{personIdentifier}/replace")]
        public ActionResult ReplaceContractPersonnel([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, string personIdentifier, [FromBody] object request, [FromQuery] bool force)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }
    }
}
