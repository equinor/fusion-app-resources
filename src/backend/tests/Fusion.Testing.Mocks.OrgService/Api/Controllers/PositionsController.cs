using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.ApiClients.Org;

namespace Fusion.Testing.Mocks.OrgService.Api.Controllers
{


    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    public class PositionsController : ControllerBase
    {

        [ApiVersion("2.0")]
        [HttpGet("/positions/{positionId}")]
        public ActionResult<ApiPositionV2> LookupPosition(Guid positionId)
        {
            var position = OrgServiceMock.positions.Union(OrgServiceMock.contractPositions)
                .FirstOrDefault(p => p.Id == positionId);

            if (position is null)
                return FusionApiError.NotFound(positionId, "Could not locate position");

            return Ok(position);
        }


        [MapToApiVersion("2.0")]
        [HttpGet("/persons/{personIdentifier}/positions")]
        public ActionResult<List<ApiPositionV2>> GetPersonPositions(string personIdentifier)
        {
            if (Guid.TryParse(personIdentifier, out Guid azureUniqueId))
            {
                return OrgServiceMock.positions.Where(p => p.Instances != null && p.Instances.Any(i => i.AssignedPerson?.AzureUniqueId == azureUniqueId)).ToList();
            }
            else
            {
                return OrgServiceMock.positions.Where(p => p.Instances != null && p.Instances.Any(i => i.AssignedPerson?.Mail.ToLower() == personIdentifier.ToLower())).ToList();
            }
        }

        [MapToApiVersion("2.0")]
        [HttpGet("/projects/{projectId}/positions/{positionId}")]
        public ActionResult<ApiPositionV2> GetPosition(string projectId, Guid positionId)
        {
            var position = OrgServiceMock.positions.FirstOrDefault(p => p.Id == positionId);

            if (position != null)
                return position;

            return NotFound();
        }

        [MapToApiVersion("2.0")]
        [HttpGet("/projects/{projectId}/contracts/{contractId}/positions/{positionId}")]
        public ActionResult<ApiPositionV2> GetPosition([FromRoute] ProjectIdentifier projectIdentifier, Guid contractId, Guid positionId)
        {
            var position = OrgServiceMock.contractPositions.FirstOrDefault(p => p.Id == positionId);

            if (position != null)
                return position;

            return NotFound();
        }

        [MapToApiVersion("2.0")]
        [HttpDelete("/projects/{projectId}/contracts/{contractId}/positions/{positionId}")]
        public ActionResult<ApiPositionV2> DeleteContractPosition([FromRoute] ProjectIdentifier projectIdentifier, Guid contractId, Guid positionId)
        {
            var position = OrgServiceMock.contractPositions.FirstOrDefault(p => p.Id == positionId);

            if (position == null)
                return NotFound();

            OrgServiceMock.contractPositions.Remove(position);

            return NoContent();
        }

        [ApiVersion("2.0")]
        [HttpPatch("projects/{projectId}/positions/{positionId}/instances/{instanceId}")]
        public ActionResult<ApiPositionV2> PatchPositionInstance([FromRoute] ProjectIdentifier projectId, Guid positionId, Guid instanceId, [FromBody] PatchInstanceRequestV2 request)
        {
            var position = OrgServiceMock.positions.FirstOrDefault(p => p.Project.ProjectId == projectId.ProjectId && p.Id == positionId);

            var instance = position?.Instances.FirstOrDefault(x => x.Id == instanceId);

            if (instance == null)
                return NotFound();

            // Do some updates based on request if required.

            return position;
        }
    }
}
