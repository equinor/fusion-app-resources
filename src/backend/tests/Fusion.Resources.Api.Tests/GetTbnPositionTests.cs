using FluentAssertions;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Resources.Domain.Queries;
using Fusion.Testing;
using Fusion.Testing.Mocks.OrgService;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Domain.Tests
{
    public class GetTbnPositionTests : IClassFixture<ResourceApiFixture>
    {
        private ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private FusionTestProjectBuilder testProject;
        private ApiPersonProfileV3 resourceOwner;

        public GetTbnPositionTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            fixture.ApiFactory.IsMemorycacheDisabled = true;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user

            testProject = new FusionTestProjectBuilder()
                .WithPositions(10)
                .AddToMockService();

            resourceOwner = fixture.AddResourceOwner("PDP PRD FE SE");
        }

        [Fact]
        public async Task GetTbn_ShouldIgnorePositionsAboveResourceOwner()
        {
            using var userScope = fixture.UserScope(resourceOwner);

            var expectedTbnPosition = testProject.AddPosition()
                .WithEnsuredFutureInstances()
                .WithNoAssignedPerson();
            expectedTbnPosition.BasePosition.Department = "PDP PRD FE SE L5";

            var positionAboveLeader = testProject
                .AddPosition()
                .WithEnsuredFutureInstances()
                .WithNoAssignedPerson();
            positionAboveLeader.BasePosition.Department = "PDP PRD";

            var client = fixture.ApiFactory.CreateClient();
            var response = await client.TestClientGetAsync($"departments/{resourceOwner.FullDepartment}/resources/requests/tbn", new[]
            {
                new { PositionId = Guid.Empty, BasePosition = new { Department = "" } }
            });

            var tbns = response.Value;
            tbns.Should().NotContain(x => x.PositionId == positionAboveLeader.Id);
            tbns.Should().Contain(x => x.PositionId == expectedTbnPosition.Id);
            tbns.Should().OnlyContain(x => x.BasePosition.Department.StartsWith(resourceOwner.FullDepartment));
        }

        [Fact]
        public async Task GetTbn_ShouldIgnoreExpiredPositions()
        {
            using var userScope = fixture.UserScope(resourceOwner);

            var expectedTbnPosition = testProject.AddPosition()
                .WithEnsuredFutureInstances()
                .WithNoAssignedPerson();
            expectedTbnPosition.BasePosition.Department = "PDP PRD FE SE L5";

            var expiredPosition = testProject
                .AddPosition()
                .WithInstances(x =>
                {
                    var expiredInstance = x.AddInstance(TimeSpan.Zero);
                    expiredInstance.AppliesFrom = new DateTime(2020, 04, 01);
                    expiredInstance.AppliesTo = new DateTime(2021, 04, 01);
                    expiredInstance.AssignedPerson = null;
                });
                
            expiredPosition.BasePosition.Department = "PDP PRD FE SE";

            var client = fixture.ApiFactory.CreateClient();
            var response = await client.TestClientGetAsync($"departments/{resourceOwner.FullDepartment}/resources/requests/tbn", new[]
            {
                new { PositionId = Guid.Empty, BasePosition = new { Department = "" } }
            });

            var tbns = response.Value;
            tbns.Should().NotContain(x => x.PositionId == expiredPosition.Id);
            tbns.Should().Contain(x => x.PositionId == expectedTbnPosition.Id);
            tbns.Should().OnlyContain(x => x.BasePosition.Department.StartsWith(resourceOwner.FullDepartment));
        }
    }
}
