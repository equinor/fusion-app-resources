using Microsoft.AspNetCore.Mvc;
using Fusion.ApiClients.Org;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Testing.Mocks.OrgService.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    public class DraftsController : ControllerBase
    {
        private static List<ApiDraftV2> drafts = new List<ApiDraftV2>();

        [HttpPost("/projects/{projectIdentifier}/drafts")]
        public ActionResult<ApiDraftV2> CreateDraft([FromRoute] ProjectIdentifier projectIdentifier, [FromBody] NewDraftRequest request)
        {
            var project = projectIdentifier.GetProjectOrDefault();

            if (project is null)
                return NotFound();


            var draft = new ApiDraftV2()
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Created = DateTime.UtcNow,
                Project = new ApiProjectReferenceV2()
                {
                    DomainId = project.DomainId,
                    Name = project.Name,
                    ProjectId = project.ProjectId,
                    ProjectType = project.ProjectType
                },
                ProjectId = project.ProjectId,
                Status = "new",
                CreatedBy = new ApiPersonV2()
                {
                    AccountType = "Application",
                    AzureUniqueId = Guid.NewGuid(),
                    Name = "Fake Draft creator"
                }
            };

            lock(drafts)
            {
                drafts.Add(draft);
            }

            return draft;
        }

        [HttpPost("/projects/{projectIdentifier}/drafts/{draftId}/publish")]
        public ActionResult<ApiDraftV2> PublishDraft([FromRoute] ProjectIdentifier projectIdentifier, Guid draftId)
        {
            var draft = drafts.FirstOrDefault(d => d.Id == draftId);
            if (draft == null)
                return NotFound();

            draft.PublishStartedDate = DateTime.UtcNow;
            draft.Status = "Published";

            return draft;
        }


        [HttpDelete("/drafts/{draftId}")]
        [HttpDelete("/projects/{projectIdentifier}/drafts/{draftId}")]
        public ActionResult<ApiDraftV2> DeleteDraft([FromRoute] ProjectIdentifier projectIdentifier, Guid draftId)
        {
            return NoContent();
        }


        [HttpGet("/projects/{projectIdentifier}/drafts/{draftId}")]
        public ActionResult<ApiDraftV2> GetDraft([FromRoute] ProjectIdentifier projectIdentifier, Guid draftId)
        {
            var draft = drafts.FirstOrDefault(d => d.Id == draftId);
            if (draft != null)
                return draft;

            return NotFound();
        }


        [HttpGet("/drafts/{draftId}/publish")]
        public ActionResult<ApiDraftV2> GetDraftPublishingStatus([FromRoute] ProjectIdentifier projectIdentifier, Guid draftId)
        {
            var draft = drafts.FirstOrDefault(d => d.Id == draftId);
            if (draft == null)
                return NotFound();

            return draft;
        }
    }

    public class NewDraftRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
