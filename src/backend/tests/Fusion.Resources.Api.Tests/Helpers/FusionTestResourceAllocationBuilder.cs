using System;
using System.Linq;
using Bogus;
using Fusion.ApiClients.Org;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Controllers;
using Fusion.Testing.Mocks.OrgService;

namespace Fusion.Testing.Mocks
{
    public class FusionTestResourceAllocationBuilder
    {
        public ApiProjectV2 Project { get; set; }

        public CreateProjectAllocationRequest Request { get; }

        public FusionTestResourceAllocationBuilder()
        {
            Project = OrgTestData.Project().Generate();
            Request = ResourceAllocationTestData.Request().Generate();
        }

        public FusionTestResourceAllocationBuilder(CreateProjectAllocationRequest request)
        {
            this.Request = request;
        }

        public FusionTestResourceAllocationBuilder WithOrgPositionId(ApiPositionV2 position)
        {
            this.Request.OrgPositionId = position.Id;
            this.Request.OrgPositionInstance.Id = position.Instances.First().Id;
            return this;
        }
        public FusionTestResourceAllocationBuilder WithProject(ApiProjectV2 project)
        {
            Project = project;
            return this;
        }
        public FusionTestResourceAllocationBuilder WithProposedPerson(ApiPersonProfileV3 profile)
        {
            Request.ProposedPersonId = profile.AzureUniqueId.Value;
            return this;
        }
    }
}
