using System.Linq;
using Fusion.ApiClients.Org;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Controllers;
using Fusion.Testing.Mocks.OrgService;

namespace Fusion.Testing.Mocks
{
    public class FusionTestResourceAllocationBuilder
    {
        public ApiProjectV2 Project { get; set; }

        public CreateResourceAllocationRequest Request { get; }
        public FusionTestResourceAllocationBuilder()
        {
            Project = OrgTestData.Project().Generate();
            Request = ResourceAllocationTestData.Request().Generate();
        }

        public FusionTestResourceAllocationBuilder(CreateResourceAllocationRequest request)
        {
            this.Request = request;
        }

        public FusionTestResourceAllocationBuilder WithOrgPositionId(ApiPositionV2 position)
        {
            this.Request.OrgPositionId = position.Id;
            this.Request.OrgPositionInstance = new ApiPositionInstance(position.Instances.First());
            return this;
        }
        public FusionTestResourceAllocationBuilder WithProject(ApiProjectV2 project)
        {
            Project = project;
            Request.ProjectId = Project.ProjectId;
            return this;
        }
        public FusionTestResourceAllocationBuilder WithIsDraft(bool isDraft)
        {
            this.Request.IsDraft = isDraft;
            return this;
        }
        public FusionTestResourceAllocationBuilder WithProposedPerson(ApiPersonProfileV3 profile)
        {
            Request.ProposedPersonAzureUniqueId = profile.AzureUniqueId.Value;
            return this;
        }
        public FusionTestResourceAllocationBuilder WithProposedChanges(ApiPropertiesCollection changes)
        {
            Request.ProposedChanges = changes;
            return this;
        }

        public FusionTestResourceAllocationBuilder WithRequestType(ApiAllocationRequestType type)
        {
            Request.Type = type;
            return this;
        }
    }
}
