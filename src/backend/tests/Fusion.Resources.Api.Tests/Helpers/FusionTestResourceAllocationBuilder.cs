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

        public CreateResourceAllocationRequest CreateRequest { get; }
        public FusionTestResourceAllocationBuilder()
        {
            Project = OrgTestData.Project().Generate();
            CreateRequest = ResourceAllocationTestData.Request().Generate();
        }

        public FusionTestResourceAllocationBuilder(CreateResourceAllocationRequest createRequest)
        {
            this.CreateRequest = createRequest;
        }

        public FusionTestResourceAllocationBuilder WithOrgPositionId(ApiPositionV2 position)
        {
            this.CreateRequest.OrgPositionId = position.Id;
            this.CreateRequest.OrgPositionInstance = new ApiPositionInstance(position.Instances.First());
            return this;
        }
        public FusionTestResourceAllocationBuilder WithProject(ApiProjectV2 project)
        {
            Project = project;
            CreateRequest.ProjectId = Project.ProjectId;
            return this;
        }
        public FusionTestResourceAllocationBuilder WithIsDraft(bool isDraft)
        {
            this.CreateRequest.IsDraft = isDraft;
            return this;
        }
        public FusionTestResourceAllocationBuilder WithProposedPerson(ApiPersonProfileV3 profile)
        {
            CreateRequest.ProposedPersonAzureUniqueId = profile.AzureUniqueId.Value;
            return this;
        }
        public FusionTestResourceAllocationBuilder WithProposedChanges(ApiPropertiesCollection changes)
        {
            CreateRequest.ProposedChanges = changes;
            return this;
        }

        public FusionTestResourceAllocationBuilder WithRequestType(ApiAllocationRequestType type)
        {
            CreateRequest.Type = type;
            return this;
        }
    }
}
