using Fusion.AspNetCore.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.AspNetCore.Http;

namespace Fusion.Resources.Api.Controllers
{
    [Authorize]
    [ApiController]
    public class RequestsController : ResourceControllerBase
    {
        /// <summary>
        /// 
        /// OData:
        ///     $expand = originalPosition
        ///     
        /// </summary>
        /// <param name="projectIdentifier"></param>
        /// <param name="contractIdentifier"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        [Obsolete]
        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests")]
        public ActionResult GetContractRequests([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromQuery] ODataQueryParams query)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpGet("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public ActionResult GetContractRequestById([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, [FromQuery] ODataQueryParams query)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }


        /// <summary>
        /// 
        /// 
        /// Validations:
        /// - Only one change request for a specific position can be active at the same time.
        ///    -> Bad Request, Invalid operation.
        ///    
        /// - The original position id has to be a valid position.
        ///     -> Bad Request
        /// </summary>
        /// <param name="projectIdentifier"></param>
        /// <param name="contractIdentifier"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [Obsolete]
        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests")]
        public ActionResult CreatePersonnelRequest([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, [FromBody] object request)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public ActionResult UpdatePersonnelRequest([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, [FromBody] object request)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/approve")]
        public ActionResult ApproveContractorPersonnelRequest([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/reject")]
        public ActionResult RejectContractorPersonnelRequest([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, [FromBody] object request)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [HttpDelete("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public ActionResult DeleteContractorRequestById([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/provision")]
        public ActionResult ProvisionContractorRequest([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        #region Comments

        [Obsolete]
        [HttpPost("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/comments")]
        public ActionResult AddRequestComment([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, [FromBody] RequestCommentRequest create)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpPut("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/comments/{commentId}")]
        public ActionResult UpdateRequestComment(
            [FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, Guid commentId, [FromBody] RequestCommentRequest update)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpDelete("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/comments/{commentId}")]
        public ActionResult DeleteRequestComment([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, Guid commentId)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        #endregion Comments

        #region Options

        [Obsolete]
        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests")]
        public ActionResult CheckAccessCreateRequests([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}")]
        public ActionResult CheckAccessUpdateRequest([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        [Obsolete]
        [HttpOptions("/projects/{projectIdentifier}/contracts/{contractIdentifier}/resources/requests/{requestId}/actions/{actionName}")]
        public ActionResult CheckAccessRequestAction([FromRoute] PathProjectIdentifier projectIdentifier, Guid contractIdentifier, Guid requestId, string actionName)
        {
            return StatusCode(StatusCodes.Status410Gone);
        }

        #endregion
    }
}
