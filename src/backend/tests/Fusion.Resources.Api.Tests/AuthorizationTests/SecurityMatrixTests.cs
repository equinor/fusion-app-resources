using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.AuthorizationTests
{
    public class SecurityMatrixTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        const string TestDepartment = "TPD PRD TST ASD";
        const string SiblingDepartment = "TPD PRD TST FGH";
        const string ParentDepartment = "TPD PRD TS";

        const string SameL2Department = "TPD PRD FE MMS STR1";

        private ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private FusionTestProjectBuilder testProject;
        private ApiClients.Org.ApiPositionV2 testPosition;
        private OrgRequestInterceptor creatorInterceptor;

        public Dictionary<string, ApiPersonProfileV3> Users { get; private set; }

        public SecurityMatrixTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            fixture.DisableMemoryCache();

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            var creator = fixture.AddProfile(FusionAccountType.Employee);
            var resourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            resourceOwner.IsResourceOwner = true;

            var resourceOwnerCreator = fixture.AddProfile(FusionAccountType.Employee);
            resourceOwnerCreator.IsResourceOwner = true;
            resourceOwnerCreator.FullDepartment = TestDepartment;

            testProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .AddToMockService();

            fixture.ContextResolver
               .AddContext(testProject.Project);

            var bp = testProject.AddBasePosition($"{Guid.NewGuid()}", s => s.Department = TestDepartment);
            testPosition = testProject.AddPosition().WithBasePosition(bp);

            Users = new Dictionary<string, ApiPersonProfileV3>()
            {
                ["creator"] = creator,
                ["resourceOwner"] = resourceOwner,
                ["resourceOwnerCreator"] = resourceOwnerCreator
            };
        }

        public Task InitializeAsync() => Task.CompletedTask;

        [Theory]
        //TODO: [InlineData("resourceOwner", TestDepartment, true)]
        //TODO: [InlineData("resourceOwner", SiblingDepartment, true)]
        //TODO: [InlineData("resourceOwner", ParentDepartment, true)]
        //TODO: [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("creator", "TPD RND WQE FQE", true)]
        public async Task CanDeleteRequestAssignedToDepartment(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientDeleteAsync<dynamic>($"/projects/{testProject.Project.ProjectId}/requests/{request.Id}");

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        //TODO: [InlineData("resourceOwner", ParentDepartment, true)]
        //TODO: [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("creator", "TPD RND WQE FQE", true)]

        public async Task CanReadRequestsAssignedToDepartment(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientGetAsync<TestApiInternalRequestModel>($"/projects/{testProject.Project.ProjectId}/requests/{request.Id}");

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        //TODO: [InlineData("resourceOwner", ParentDepartment, true)]
        //TODO: [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("creator", "TPD RND WQE FQE", true)]

        public async Task CanEditGeneralOnRequestAssignedToDepartment(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                $"/projects/{testProject.Project.ProjectId}/requests/{request.Id}",
                new
                {
                    proposedChanges = new
                    {
                        location = new { id = Guid.NewGuid(), name = "Test location" }
                    }
                }
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        //TODO: [InlineData("resourceOwner", ParentDepartment, true)]
        //TODO: [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("creator", "TPD RND WQE FQE", true)]

        public async Task CanEditAdditionalCommentOnRequestAssignedToDepartment(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                $"/projects/{testProject.Project.ProjectId}/requests/{request.Id}",
                new
                {
                    additionalComment = "updated comment"
                }
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        //TODO: [InlineData("resourceOwner", ParentDepartment, true)]
        //TODO: [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("creator", "TPD RND WQE FQE", false)]

        public async Task CanReassignOnRequestAssignedToDepartment(string role, string department, bool shouldBeAllowed)
        {
            const string changedDepartment = "TPD UPD ASD";
            fixture.EnsureDepartment(changedDepartment);

            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                $"/projects/{testProject.Project.ProjectId}/requests/{request.Id}",
                new { assignedDepartment = changedDepartment }
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, false)]
        [InlineData("resourceOwner", SiblingDepartment, false)]
        //TODO: [InlineData("resourceOwner", ParentDepartment, true)]
        //TODO: [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("creator", "TPD RND WQE FQE", true)]
        public async Task CanStartNormalRequest(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPostAsync<dynamic>(
                $"/projects/{testProject.Project.ProjectId}/requests/{request.Id}/start", null
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        //TODO: [InlineData("resourceOwner", ParentDepartment, true)]
        //TODO: [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("creator", "TPD RND WQE FQE", false)]
        public async Task CanProposeNormalRequest(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;
            using var userScope = fixture.UserScope(Users[role]);

            var proposedPerson = PeopleServiceMock.AddTestProfile()
                .SaveProfile();

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPatchAsync<TestApiInternalRequestModel>(
                $"/resources/requests/internal/{request.Id}",
                new { proposedPersonAzureUniqueId = proposedPerson.AzureUniqueId });

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, false)]
        [InlineData("resourceOwner", SiblingDepartment, false)]
        //TODO: [InlineData("resourceOwner", ParentDepartment, true)]
        //TODO: [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("creator", "TPD RND WQE FQE", true)]
        public async Task CanAcceptNormalRequest(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateAndStartRequest();
            Users[role].FullDepartment = department;

            using (var adminScope = fixture.AdminScope())
            {
                var proposedPerson = PeopleServiceMock.AddTestProfile()
                    .SaveProfile();

                var adminClient = fixture.ApiFactory.CreateClient();
                await adminClient.ProposePersonAsync(request.Id, proposedPerson);
            }

            using var userScope = fixture.UserScope(Users[role]);

            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPostAsync<TestApiInternalRequestModel>(
                $"/projects/{testProject.Project.ProjectId}/resources/requests/{request.Id}/approve",
                null
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwner", TestDepartment, true)]
        [InlineData("resourceOwner", SiblingDepartment, true)]
        //TODO: [InlineData("resourceOwner", ParentDepartment, true)]
        //TODO: [InlineData("resourceOwner", SameL2Department, true)]
        [InlineData("resourceOwnerCreator", TestDepartment, true)]
        public async Task CanStartChangeRequest(string role, string department, bool shouldBeAllowed)
        {
            var request = await CreateChangeRequest(TestDepartment);

            var client = fixture.ApiFactory.CreateClient();
            using (var adminscope = fixture.AdminScope())
            {
                var testUser = fixture.AddProfile(FusionAccountType.Employee);

                await client.SetChangeParamsAsync(request.Id, DateTime.Today.AddDays(1));
                await client.ProposePersonAsync(request.Id, testUser);
            }
            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var result = await client.TestClientPostAsync<TestApiInternalRequestModel>(
                $"/departments/{department}/resources/requests/{request.Id}/start",
                null
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        [Theory]
        [InlineData("resourceOwnerCreator", TestDepartment, false)]
        public async Task CanAcceptChangeRequest(string role, string department, bool shouldBeAllowed)
        {
            var chgRequest = await CreateChangeRequest(department);

            using var userScope = fixture.UserScope(Users[role]);

            Users[role].FullDepartment = department;
            var client = fixture.ApiFactory.CreateClient();
            var result = await client.TestClientPostAsync<TestApiInternalRequestModel>(
                $"/departments/{chgRequest.AssignedDepartment}/requests/{chgRequest.Id}/approve",
                null
            );

            if (shouldBeAllowed) result.Should().BeSuccessfull();
            else result.Should().BeUnauthorized();
        }

        private async Task<TestApiInternalRequestModel> CreateChangeRequest(string department)
        {
            var creatorClient = fixture.ApiFactory.CreateClient()
                            .WithTestUser(Users["resourceOwnerCreator"])
                            .AddTestAuthToken();

            using var i = creatorInterceptor = OrgRequestMocker
                 .InterceptOption($"/{testPosition.Id}")
                 .RespondWithHeaders(HttpStatusCode.NoContent, h => h.Add("Allow", "PUT"));

            var req = await creatorClient.CreateDefaultResourceOwnerRequestAsync(
                department, testProject, 
                r => r.AsTypeResourceOwner("changeResource"), 
                p => p.WithAssignedPerson(fixture.AddProfile(FusionAccountType.Employee))
            );

            return req;
        }

        private async Task<TestApiInternalRequestModel> CreateAndStartRequest()
        {
            var creatorClient = fixture.ApiFactory.CreateClient()
                            .WithTestUser(Users["creator"])
                            .AddTestAuthToken();

            using var i = creatorInterceptor = OrgRequestMocker
                 .InterceptOption($"/{testPosition.Id}")
                 .RespondWithHeaders(HttpStatusCode.NoContent, h => h.Add("Allow", "PUT"));

            return await creatorClient.CreateAndStartDefaultRequestOnPositionAsync(testProject, testPosition);
        }

        private async Task<TestApiInternalRequestModel> CreateRequest()
        {
            var creatorClient = fixture.ApiFactory.CreateClient()
                            .WithTestUser(Users["creator"])
                            .AddTestAuthToken();

            using var i = creatorInterceptor = OrgRequestMocker
                 .InterceptOption($"/{testPosition.Id}")
                 .RespondWithHeaders(HttpStatusCode.NoContent, h => h.Add("Allow", "PUT"));

            return await creatorClient.CreateRequestAsync(testProject.Project.ProjectId,
                req => req.AsTypeNormal().WithPosition(testPosition)
            );
        }


        public Task DisposeAsync()
        {
            creatorInterceptor.Dispose();
            loggingScope.Dispose();

            return Task.CompletedTask;
        }
    }
}
