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
        [HttpGet("/positions/basepositions/{basepositionId}")]
        public ActionResult<ApiBasePositionV2> GetBaseposition(Guid basepositionId)
        {
            var bp = PositionBuilder.AllBasePositions.FirstOrDefault(bp => bp.Id == basepositionId);
            if (bp is null) return NotFound();

            return Ok(bp);
        }

        [ApiVersion("2.0")]
        [HttpGet("/positions/{positionId}")]
        public ActionResult<ApiPositionV2> LookupPosition(Guid positionId)
        {
            OrgServiceMock.contractPositions.TryGetValue(positionId, out ApiPositionV2 position);

            if (position is null)
                position = OrgServiceMock.positions.FirstOrDefault(x => x.Id == positionId);

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

        [ApiVersion("2.0")]
        [Produces("application/json")]
        [HttpGet("/projects/{projectId}/contracts/{contractId}/positions")]
        [HttpGet("/projects/{projectId}/drafts/{draftId}/contracts/{contractId}/positions")]
        public ActionResult<List<ApiPositionV2>> GetContractPositions([FromRoute] ProjectIdentifier projectId, Guid contractId, [FromRoute] Guid? draftId)
        {
            return (from pos
                in OrgServiceMock.contractPositions
                    where pos.Value.ContractId == contractId && pos.Value.ProjectId == projectId.ProjectId
                    select pos.Value).ToList();
        }

        [MapToApiVersion("2.0")]
        [HttpGet("/projects/{projectId}/contracts/{contractId}/positions/{positionId}")]
        [HttpGet("/projects/{projectId}/drafts/{draftId}/contracts/{contractId}/positions/{positionId}")]
        public ActionResult<ApiPositionV2> GetPosition([FromRoute] ProjectIdentifier projectIdentifier, [FromRoute] Guid? draftId, Guid contractId, Guid positionId)
        {
            OrgServiceMock.contractPositions.TryGetValue(positionId, out ApiPositionV2 position);

            if (position != null)
                return position;

            return NotFound();
        }

        [MapToApiVersion("2.0")]
        [HttpDelete("/projects/{projectId}/contracts/{contractId}/positions/{positionId}")]
        public ActionResult<ApiPositionV2> DeleteContractPosition([FromRoute] ProjectIdentifier projectIdentifier, Guid contractId, Guid positionId)
        {
            OrgServiceMock.contractPositions.Remove(positionId, out ApiPositionV2 position);

            if (position == null) return NotFound();

            return NoContent();
        }

        [ApiVersion("2.0")]
        [HttpPatch("projects/{projectId}/positions/{positionId}/instances/{instanceId}")]
        [HttpPatch("projects/{projectId}/drafts/{draftId}/positions/{positionId}/instances/{instanceId}")]
        public ActionResult<ApiPositionV2> PatchPositionInstance([FromRoute] ProjectIdentifier projectId, Guid? contractId, Guid? draftId, Guid positionId, Guid? instanceId, [FromBody] PatchInstanceRequestV2 request)
        {
            ApiPositionV2 position;
            if (contractId.HasValue)
                position = OrgServiceMock.contractPositions.FirstOrDefault(p => p.Value.ProjectId == projectId.ProjectId && p.Value.ContractId == contractId && p.Value.Id == positionId).Value;
            else
                position = OrgServiceMock.positions.FirstOrDefault(p => p.Project.ProjectId == projectId.ProjectId && p.Id == positionId);

            var instance = position?.Instances.FirstOrDefault(x => x.Id == instanceId);

            if (instance == null)
                return NotFound();

            // Do some updates based on request if required.
            if (request.AppliesFrom.HasValue)
                instance.AppliesFrom = request.AppliesFrom.Value.Value;

            if (request.AppliesTo.HasValue)
                instance.AppliesTo = request.AppliesTo.Value.Value;

            if (request.Calendar.HasValue)
                instance.Calendar = request.Calendar.Value;

            if (request.ExternalId.HasValue)
                instance.ExternalId = request.ExternalId.Value;

            if (request.Location.HasValue)
                instance.Location = new ApiPositionLocationV2()
                {
                    Id = request.Location.Value.Id
                };

            if (request.AssignedPerson.HasValue)
            {
                instance.AssignedPerson = new ApiPersonV2
                {
                    AzureUniqueId = request.AssignedPerson.Value.AzureUniqueId,
                    Mail = request.AssignedPerson.Value.Mail
                };
            }

            if (request.Properties.HasValue)
            {
                if (request.Properties.Value is null)
                {
                    instance.Properties = null;
                }
                else
                {
                    instance.Properties ??= new ApiPropertiesCollectionV2();
                    foreach (var (key, value) in request.Properties.Value)
                    {
                        var existingProp = instance.Properties.FirstOrDefault(x => x.Key == key);
                        if (existingProp.Key is null)
                        {
                            instance.Properties.Add(key, value);
                        }
                        else
                        {
                            instance.Properties[key] = value;
                        }
                    }
                }
            }


            return position;
        }

        [ApiVersion("2.0")]
        [HttpPatch("projects/{projectId}/contracts/{contractId}/positions/{positionId}")]
        [HttpPatch("projects/{projectId}/drafts/{draftId}/contracts/{contractId}/positions/{positionId}")]
        public ActionResult<ApiPositionV2> PatchPosition([FromRoute] ProjectIdentifier projectId, Guid? contractId, Guid? draftId, Guid positionId, [FromBody] ApiPositionV2 request)
        {
            ApiPositionV2 position;
            if (contractId.HasValue)
                position = OrgServiceMock.contractPositions.FirstOrDefault(p => p.Value.ProjectId == projectId.ProjectId && p.Value.ContractId == contractId && p.Value.Id == positionId).Value;
            else
                position = OrgServiceMock.positions.FirstOrDefault(p => p.Project.ProjectId == projectId.ProjectId && p.Id == positionId);


            foreach (var pInstance in request.Instances)
            {
                var instance = position?.Instances.FirstOrDefault(x => x.Id == pInstance.Id);

                if (instance == null)
                    continue;

                // Do some updates based on request if required.
                instance.AppliesFrom = pInstance.AppliesFrom;
                instance.AppliesTo = pInstance.AppliesTo;
                instance.Calendar = pInstance.Calendar;
                instance.ExternalId = pInstance.ExternalId;
                if (pInstance.Location != null)
                    instance.Location = new ApiPositionLocationV2()
                    {
                        Id = pInstance.Location.Id
                    };

                if (pInstance.AssignedPerson != null)
                    instance.AssignedPerson = new ApiPersonV2
                    {
                        AzureUniqueId = pInstance.AssignedPerson.AzureUniqueId,
                        Mail = pInstance.AssignedPerson.Mail
                    };
            }
            return position;
        }

        [MapToApiVersion("2.0")]
        [HttpGet("/projects/{projectId}/positions/{positionId}/task-owner")]
        public ActionResult<ApiPositionV2> GetPositionTaskOwner([FromRoute] ProjectIdentifier projectId, Guid positionId)
        {
            if (OrgServiceMock.taskOwnerMapping.TryGetValue(positionId, out Guid taskOwnerPositionId))
            {
                var taskOwner = OrgServiceMock.positions.FirstOrDefault(p => p.Project.ProjectId == projectId.ProjectId && p.Id == taskOwnerPositionId);
                if (taskOwner is null)
                    return NoContent();

                return taskOwner;
            }

            return OrgServiceMock.projects.FirstOrDefault(p => p.ProjectId == projectId.ProjectId).Director;
        }

        [MapToApiVersion("2.0")]
        [HttpGet("/projects/{projectId}/positions/{positionId}/instances/{instanceId}/task-owner")]
        public ActionResult<MockApiTaskOwnerV2> GetInstanceTaskOwner([FromRoute] ProjectIdentifier projectId, Guid positionId, Guid instanceId)
        {
            if (OrgServiceMock.taskOwnerMapping.TryGetValue(positionId, out Guid taskOwnerPositionId))
            {
                var position = OrgServiceMock.GetPosition(positionId);
                if (position is null)
                    return NotFound(new { error = new { message = "Could not locate position" } });

                var instance = position.Instances.FirstOrDefault(i => i.Id == instanceId);
                if (instance is null)
                    return NotFound(new { error = new { message = "Could not locate instance" } });


                var taskOwner = OrgServiceMock.GetPosition(taskOwnerPositionId);

                var date = DateTime.Today;
                if (date <= instance.AppliesFrom)
                    date = instance.AppliesFrom.Date;
                if (date >= instance.AppliesTo)
                    date = instance.AppliesTo.Date;


                if (taskOwner is not null)
                {
                    var taskOwnerResp = new MockApiTaskOwnerV2(date, taskOwner);
                    return taskOwnerResp;
                }
            }

            var director = OrgServiceMock.GetProject(projectId.ProjectId.Value)?.Director;
            return new MockApiTaskOwnerV2(director);
        }

        [MapToApiVersion("2.0")]
        [HttpGet("/projects/{projectId}/positions/{positionId}/instances/{instanceId}/reports-to")]
        public ActionResult GetPositionReportsTo([FromRoute] ProjectIdentifier projectId, Guid positionId, Guid instanceId)
        {
            var director = OrgServiceMock.GetProject(projectId.ProjectId.Value)?.Director;

            if (OrgServiceMock.taskOwnerMapping.TryGetValue(positionId, out Guid taskOwnerPositionId))
            {
                var position = OrgServiceMock.GetPosition(positionId);
                if (position is null)
                    return NotFound(new { error = new { message = "Could not locate position" } });

                var instance = position.Instances.FirstOrDefault(i => i.Id == instanceId);
                if (instance is null)
                    return NotFound(new { error = new { message = "Could not locate instance" } });


                var taskOwner = OrgServiceMock.GetPosition(taskOwnerPositionId);
                taskOwner.IsTaskOwner = true;

                var date = DateTime.Today;
                if (date <= instance.AppliesFrom)
                    date = instance.AppliesFrom.Date;
                if (date >= instance.AppliesTo)
                    date = instance.AppliesTo.Date;


                if (taskOwner is not null)
                {
                    return Ok(new
                    {
                        Path = new[] { taskOwner.Id, director.Id },
                        ReportPositions = new[] { director, taskOwner }
                    });
                }
            }

            return Ok(new
            {
                Path = new[] { director.Id },
                ReportPositions = new[] { director }
            });
        }

        public class MockApiTaskOwnerV2
        {
            public MockApiTaskOwnerV2(ApiPositionV2 taskOwner) : this(DateTime.Today, taskOwner)
            {
            }
            public MockApiTaskOwnerV2(DateTime date, ApiPositionV2 taskOwner)
            {
                Date = date;
                PositionId = taskOwner.Id;

                var instances = taskOwner.Instances.Where(i => i.AppliesFrom.Date <= Date && i.AppliesTo.Date >= Date).ToList();

                if (instances.Count == 0 && taskOwner.Instances.Any())
                    instances.Add(taskOwner.Instances.OrderBy(i => i.AppliesTo).Last());

                if (instances is not null)
                {
                    InstanceIds = instances.Select(i => i.Id).ToArray();
                    Persons = instances.Where(i => i.AssignedPerson is not null).Select(i => i.AssignedPerson).ToArray();
                }
            }

            /// <summary>
            /// The date used to resolve the task owner.
            /// </summary>
            public DateTime Date { get; set; }

            /// <summary>
            /// The position id of the task owner
            /// </summary>
            public Guid? PositionId { get; set; }

            /// <summary>
            /// Instances that are active at the date. This is usually related to rotations.
            /// Could also be delegated responsibility.
            /// </summary>
            public Guid[] InstanceIds { get; set; }

            /// <summary>
            /// The persons assigned to the resolved instances.
            /// </summary>
            public ApiPersonV2[] Persons { get; set; }
        }
    }
}
