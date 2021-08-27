using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Resources.Domain;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.LineOrgService;
using Fusion.Testing.Mocks.OrgService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class RequestComments : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        const string testDepartment = "L1 L2 L3 L4";
        private ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private FusionTestProjectBuilder testProject;
        private TestApiInternalRequestModel request;
        private Guid commentId;

        private ApiPersonProfileV3 testUser;
        private ApiPersonProfileV3 resourceOwner;
        private ApiPersonProfileV3 taskOwner;

        public RequestComments(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.Employee);
            testUser.FullDepartment = testDepartment;
            testUser.Department = "L2 L3 L4";

            resourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            resourceOwner.FullDepartment = testUser.FullDepartment;
            resourceOwner.IsResourceOwner = true;
            LineOrgServiceMock.AddTestUser().MergeWithProfile(resourceOwner).SaveProfile();

            taskOwner = fixture.AddProfile(FusionAccountType.Employee);
        }

        public async Task InitializeAsync()
        {
            // Mock project
            testProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .AddToMockService();

            var taskOwnerPosition = PositionBuilder.NewPosition();
            taskOwnerPosition.IsTaskOwner = true;
            taskOwnerPosition.ProjectId = testProject.Project.ProjectId;
            taskOwnerPosition
                .WithEnsuredFutureInstances()
                .WithAssignedPerson(taskOwner);
            testProject.AddPosition(taskOwnerPosition);

            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(testProject.Project);

            // Prepare admin client
            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            // Create a default request we can work with
            request = await adminClient.CreateDefaultRequestAsync(testProject,
                r => r.AsTypeNormal(),
                pos => pos.WithParentPosition(taskOwnerPosition.Id));
            //await adminClient.StartProjectRequestAsync(testProject, request.Id);
            await adminClient.StartProjectRequestAsync(testProject, request.Id);
            await adminClient.AssignDepartmentAsync(request.Id, resourceOwner.FullDepartment);

            var comment = new { content = "Resource owner gossip." };
            var response = await adminClient.TestClientPostAsync<TestApiComment>($"/resources/requests/internal/{request.Id}/comments", comment);

            commentId = response.Value.Id;
        }

        [Fact]
        public async Task ShouldBeIncludedForResourceOwner()
        {
            var client = fixture.ApiFactory
                .CreateClient()
                .WithTestUser(resourceOwner)
                .AddTestAuthToken();

            var comments = await client.TestClientGetAsync<List<TestApiComment>>($"/resources/requests/internal/{request.Id}/comments");

            comments.Value.Should().HaveCount(1);
            comments.Value.Should().Contain(x => x.Id == commentId);
        }


        [Fact]
        public async Task ShouldBeIncludedForParentResourceOwner()
        {
            var parentResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            parentResourceOwner.FullDepartment = new DepartmentPath(testDepartment).Parent();
            parentResourceOwner.IsResourceOwner = true;

            var client = fixture.ApiFactory
                .CreateClient()
                .WithTestUser(resourceOwner)
                .AddTestAuthToken();

            var comments = await client.TestClientGetAsync<List<TestApiComment>>($"/resources/requests/internal/{request.Id}/comments");

            comments.Value.Should().HaveCount(1);
            comments.Value.Should().Contain(x => x.Id == commentId);
        }

        [Fact]
        public async Task ShouldBeIncludedForSiblingResourceOwner()
        {
            var siblingResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            siblingResourceOwner.FullDepartment = new DepartmentPath(testDepartment).Parent() + " QWE";
            siblingResourceOwner.IsResourceOwner = true;

            var client = fixture.ApiFactory
                .CreateClient()
                .WithTestUser(resourceOwner)
                .AddTestAuthToken();

            var comments = await client.TestClientGetAsync<List<TestApiComment>>($"/resources/requests/internal/{request.Id}/comments");

            comments.Value.Should().HaveCount(1);
            comments.Value.Should().Contain(x => x.Id == commentId);
        }

        [Fact]
        public async Task ShouldBeExcludedForTaskOwner()
        {
            var client = fixture.ApiFactory
                .CreateClient()
                .WithTestUser(taskOwner)
                .AddTestAuthToken();

            var comments = await client.TestClientGetAsync<List<TestApiComment>>($"/resources/requests/internal/{request.Id}/comments");

            comments.Response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task ShouldBeForbiddenWhenCompleted()
        {
            var client = fixture.ApiFactory
                .CreateClient()
                .WithTestUser(resourceOwner)
                .AddTestAuthToken();

            var proposed = fixture.AddProfile(FusionAccountType.Employee);


            using (var scope = fixture.AdminScope())
            {
                await client.ProposePersonAsync(request.Id, proposed);
                await client.ResourceOwnerApproveAsync(testDepartment, request.Id);
                await client.TaskOwnerApproveAsync(testProject, request.Id);
                await client.ProvisionRequestAsync(request.Id);
            }

            var comments = await client.TestClientGetAsync<List<TestApiComment>>($"/resources/requests/internal/{request.Id}/comments");

            comments.Response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ShouldBeHiddenFromOptionsWhenCompleted()
        {
            var client = fixture.ApiFactory
                .CreateClient()
                .WithTestUser(resourceOwner)
                .AddTestAuthToken();

            var proposed = fixture.AddProfile(FusionAccountType.Employee);


            using (var scope = fixture.AdminScope())
            {
                await client.ProposePersonAsync(request.Id, proposed);
                await client.ResourceOwnerApproveAsync(testDepartment, request.Id);
                await client.TaskOwnerApproveAsync(testProject, request.Id);
                await client.ProvisionRequestAsync(request.Id);
            }

            var comments = await client.TestClientOptionsAsync($"/resources/requests/internal/{request.Id}/comments");

            comments.Response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            comments.Response.Headers.Should().NotContain(h => h.Key == "Allow" && h.Value.Contains("GET"));
        }

        [Fact]
        public async Task ShouldBeHiddenFromAdminOptionsWhenCompleted()
        {
            var client = fixture.ApiFactory
                .CreateClient()
                .WithTestUser(resourceOwner)
                .AddTestAuthToken();

            var proposed = fixture.AddProfile(FusionAccountType.Employee);


            using var scope = fixture.AdminScope();

            await client.ProposePersonAsync(request.Id, proposed);
            await client.ResourceOwnerApproveAsync(testDepartment, request.Id);
            await client.TaskOwnerApproveAsync(testProject, request.Id);
            await client.ProvisionRequestAsync(request.Id);

            var comments = await client.TestClientOptionsAsync($"/resources/requests/internal/{request.Id}/comments");

            comments.Response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            comments.Response.Headers.Should().NotContain(h => h.Key == "Allow" && h.Value.Contains("GET"));
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
