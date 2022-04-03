using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
#nullable enable 

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class ResourceOwnerRequestTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private const string SUBTYPE_CHANGE = "changeResource";
        private const string SUBTYPE_REMOVE = "removeResource";
        private const string SUBTYPE_ADJUST = "adjustment";

        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;

        /// <summary>
        ///     Will be generated new for each test
        /// </summary>
        private ApiPersonProfileV3 testUser;


        // Created by the async lifetime
        private TestApiInternalRequestModel adjustmentRequest = null!;
        private FusionTestProjectBuilder testProject = null!;
        private string testDepartment = null!;

        private Guid projectId => testProject.Project.ProjectId;

        public ResourceOwnerRequestTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.External);
        }

        private HttpClient Client => fixture.ApiFactory.CreateClient();

        public async Task InitializeAsync()
        {
            // Mock profile
            testUser = PeopleServiceMock.AddTestProfile()
                .SaveProfile();

            // Mock project
            testProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .WithProperty("pimsWriteSyncEnabled", true)
                .AddToMockService();

            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(testProject.Project);

            // Prepare admin client
            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            testDepartment = InternalRequestData.PickRandomDepartment();
            // Create a default request we can work with

            var assignedPerson = PeopleServiceMock.AddTestProfile().WithAccountType(FusionAccountType.Employee).WithFullDepartment(testDepartment).WithDepartment(testDepartment).SaveProfile();
            // Create adjustment request on a position instance currently active
            adjustmentRequest = await adminClient.CreateDefaultResourceOwnerRequestAsync(
                testDepartment, testProject,
                r => r.AsTypeResourceOwner(SUBTYPE_ADJUST),
                p => p.WithAssignedPerson(assignedPerson)
            );
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }

        #region Create request tests

        [Fact]
        public async Task CreateRequest_ShouldBeBadRequest_WhenNoPosition()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientPostAsync($"/departments/{testDepartment}/resources/requests", new { type = "ResourceOwnerChange", subtype = SUBTYPE_ADJUST }, new { Id = Guid.Empty });

            response.Should().BeBadRequest();
        }

        [Theory]
        [InlineData("allocation")]
        public async Task CreateRequest_ShouldBeBadRequest_WhenTypeIs(string type)
        {
            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition();

            var response = await Client.TestClientPostAsync($"/departments/{testDepartment}/resources/requests", new
            {
                type = type,
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id
            }, new { Id = Guid.Empty });

            response.Should().BeBadRequest();
        }



        [Fact]
        public async Task CreateRequest_ShouldBeSuccessfull_WhenExitingPosition()
        {
            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition();
            var assignedPerson = PeopleServiceMock.AddTestProfile().WithAccountType(FusionAccountType.Employee).WithFullDepartment(testDepartment).WithDepartment(testDepartment).SaveProfile();
            position.WithAssignedPerson(assignedPerson);
            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{testDepartment}/resources/requests", new
            {
                type = "resourceOwnerChange",
                subType = "adjustment",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id
            });
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task NormalRequest_Create_ShouldHaveIsDraftTrue()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/departments/{testDepartment}/resources/requests/{adjustmentRequest.Id}", new { isDraft = false });
            resp.Should().BeSuccessfull();
            resp.Value.isDraft.Should().BeTrue();
        }

        [Fact]
        public async Task ResourceOwnerRequest_ShouldNotDisplayForProject_WhenDraft()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/projects/{testProject.Project.ProjectId}/resources/requests", new { value = new[] { new { Id = Guid.Empty } } });
            resp.Should().BeSuccessfull();

            resp.Value.value.Should().NotContain(r => r.Id == adjustmentRequest.Id);
        }

        [Fact]
        public async Task ResourceOwnerRequest_Create_ShouldHaveWorkflowNull()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/departments/{testDepartment}/resources/requests/{adjustmentRequest.Id}", new { workflow = new { } });
            resp.Should().BeSuccessfull();
            resp.Value.workflow.Should().BeNull();
        }

        [Fact]
        public async Task ResourceOwnerRequest_Create_ShouldHaveStateNull()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/departments/{testDepartment}/resources/requests/{adjustmentRequest.Id}", new { state = (string?)null });
            resp.Should().BeSuccessfull();
            resp.Value.state.Should().BeNull();
        }

        #endregion

        //#region Request flow tests

        //#region Start
        [Fact]
        public async Task AdjustmentRequest_Start_ShouldBeSuccessfull_WhenChangesProposed()
        {
            using var adminScope = fixture.AdminScope();

            // Propose changes

            await Client.ProposeChangesAsync(adjustmentRequest.Id, new { workload = 50 });
            await Client.SetChangeParamsAsync(adjustmentRequest.Id, DateTime.Today.AddDays(1));

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{testDepartment}/resources/requests/{adjustmentRequest.Id}/start", null);
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task AdjustmentRequest_Start_ShouldBeBadRequest_WhenUnresolvedRequiredAction()
        {
            using var adminScope = fixture.AdminScope();

            // Propose changes

            await Client.ProposeChangesAsync(adjustmentRequest.Id, new { workload = 50 });
            await Client.SetChangeParamsAsync(adjustmentRequest.Id, DateTime.Today.AddDays(1));
            await Client.AddRequestActionAsync(adjustmentRequest.Id, x =>
            {
                x.isRequired = true;
                x.responsible = "ResourceOwner";
            });

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{testDepartment}/resources/requests/{adjustmentRequest.Id}/start", null);
            response.Should().BeBadRequest();
        }

        [Fact]
        public async Task AdjustmentRequest_Start_ShouldBeBadRequest_WhenMissingProposedChanges()
        {
            using var adminScope = fixture.AdminScope();

            await Client.SetChangeParamsAsync(adjustmentRequest.Id, DateTime.Today.AddDays(1));

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{testDepartment}/resources/requests/{adjustmentRequest.Id}/start", null);
            response.Should().BeBadRequest();
        }

        [Theory]
        [InlineData(SUBTYPE_ADJUST)]
        [InlineData(SUBTYPE_CHANGE)]
        [InlineData(SUBTYPE_REMOVE)]
        public async Task ChangeRequest_Start_ShouldBeBadRequest_WhenMissingChangeDateAndActiveInstance(string subType)
        {
            using var adminScope = fixture.AdminScope();

            var assignedPerson = PeopleServiceMock.AddTestProfile().WithAccountType(FusionAccountType.Employee).WithFullDepartment(testDepartment).WithDepartment(testDepartment).SaveProfile();
            var request = await Client.CreateDefaultResourceOwnerRequestAsync(testDepartment, testProject,
                r => r.AsTypeResourceOwner(subType),
                p => p.WithAssignedPerson(assignedPerson)
            );


            await Client.ProposeChangesAsync(request.Id, new { workload = 50 });
            await Client.ProposePersonAsync(request.Id, testUser);

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{testDepartment}/resources/requests/{request.Id}/start", null);
            response.Should().BeBadRequest();

            // The body should mention property that failed.
            response.Should().ContainErrorOnProperty("ProposalParameters");
        }

        [Fact]
        public async Task ChangeResourceRequest_Start_ShouldBeBadRequest_WhenMissingProposedPerson()
        {
            using var adminScope = fixture.AdminScope();

            var oldUser = PeopleServiceMock.AddTestProfile().WithAccountType(FusionAccountType.Employee).WithFullDepartment(testDepartment).WithDepartment(testDepartment).SaveProfile(); ;
            var newUser = PeopleServiceMock.AddTestProfile().WithAccountType(FusionAccountType.Employee).WithFullDepartment(testDepartment).WithDepartment(testDepartment).SaveProfile(); ;

            var request = await Client.CreateDefaultResourceOwnerRequestAsync(testDepartment, testProject, r => r.AsTypeResourceOwner(SUBTYPE_CHANGE), p => p.WithAssignedPerson(oldUser));

            await Client.SetChangeParamsAsync(request.Id, DateTime.Today.AddDays(1));

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{testDepartment}/resources/requests/{request.Id}/start", null);
            response.Should().BeBadRequest();

            response.Should().ContainErrorOnProperty("ProposedPerson.AzureUniqueId");
        }

        [Fact]
        public async Task ChangeResourceRequest_Start_ShouldBeSuccessfull_WhenPersonProposed()
        {
            using var adminScope = fixture.AdminScope();

            var oldUser = PeopleServiceMock.AddTestProfile().WithAccountType(FusionAccountType.Employee).WithFullDepartment(testDepartment).WithDepartment(testDepartment).SaveProfile(); ;
            var newUser = PeopleServiceMock.AddTestProfile().WithAccountType(FusionAccountType.Employee).WithFullDepartment(testDepartment).WithDepartment(testDepartment).SaveProfile(); ;

            var request = await Client.CreateDefaultResourceOwnerRequestAsync(testDepartment, testProject, r => r.AsTypeResourceOwner(SUBTYPE_CHANGE), p => p.WithAssignedPerson(oldUser));

            await Client.SetChangeParamsAsync(request.Id, DateTime.Today.AddDays(1));
            await Client.ProposePersonAsync(request.Id, newUser);

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{testDepartment}/resources/requests/{request.Id}/start", null);
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task RemoveResourceRequest_Start_ShouldBeSuccessfull_WhenChangeDateSet()
        {
            using var adminScope = fixture.AdminScope();
            var assignedPerson = PeopleServiceMock.AddTestProfile().WithAccountType(FusionAccountType.Employee).WithFullDepartment(testDepartment).WithDepartment(testDepartment).SaveProfile();

            var request = await Client.CreateDefaultResourceOwnerRequestAsync(testDepartment, testProject, r => r.AsTypeResourceOwner(SUBTYPE_REMOVE), p => p.WithAssignedPerson(assignedPerson));

            await Client.SetChangeParamsAsync(request.Id, DateTime.Today.AddDays(1));

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{testDepartment}/resources/requests/{request.Id}/start", null);
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task CreatRequestForUnassignedPositionInstance_ShouldGiveBadRequest()
        {
            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition()
                .WithInstances(1)
                .WithEnsuredFutureInstances()
                .WithNoAssignedPerson();

            var requestModel = new ApiCreateInternalRequestModel()
                .AsTypeResourceOwner()
                .WithPosition(position)
                .AsTypeResourceOwner(SUBTYPE_REMOVE);

            var newRequestResponse = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{testDepartment}/resources/requests", requestModel);
            newRequestResponse.Should().BeBadRequest();
        }

        // Is this still relevant? i.e could a position instance become unassigned between change 
        // request is created and when it is started?
        //[Fact]
        //public async Task RemoveResourceRequest_Start_ShouldBeBadRequest_WhenNoCurrentlyAssignedPersons()
        //{
        //    using var adminScope = fixture.AdminScope();

        //    var request = await Client.CreateDefaultResourceOwnerRequestAsync(testDepartment, testProject, 
        //        r => r.AsTypeResourceOwner(SUBTYPE_REMOVE), p => p.WithAssignedPerson(fixture.AddProfile(FusionAccountType.Employee))
        //    );

        //    await Client.SetChangeParamsAsync(request.Id, DateTime.Today.AddDays(1));

        //    var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{testDepartment}/resources/requests/{request.Id}/start", null);
        //    response.Should().BeBadRequest();

        //    response.Should().ContainErrorOnProperty("OrgPositionInstance.AssignedToUniqueId");
        //}

        [Fact]
        public async Task CreateChangeRequest_Should_ReturnBadRequest_WhenPimsWriteSyncNotEnabled()
        {
            // Mock project
            var disabledTestProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .WithProperty("pimsWriteSyncEnabled", false)
                .AddToMockService();

            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(disabledTestProject.Project);


            using var adminScope = fixture.AdminScope();
            var assignedPerson = PeopleServiceMock.AddTestProfile().WithAccountType(FusionAccountType.Employee).WithFullDepartment(testDepartment).WithDepartment(testDepartment).SaveProfile();

            var position = disabledTestProject.AddPosition()
                .WithAssignedPerson(assignedPerson)
                .WithEnsuredFutureInstances();

            var response = await Client.TestClientPostAsync<TestApiInternalRequestModel>($"/departments/{testDepartment}/resources/requests", new
            {
                type = "resourceOwnerChange",
                subType = "adjustment",
                orgPositionId = position.Id,
                orgPositionInstanceId = position.Instances.Last().Id
            });

            response.Should().BeBadRequest();

            var error = JsonConvert.DeserializeAnonymousType(response.Content, new { error = new { code = string.Empty, message = string.Empty } });
            error!.error.code.Should().Be("ChangeRequestsDisabled");
        }


        [Fact]
        public async Task CheckChangeRequest_Should_BeDisabled_When_PimsWriteSyncNotEnabled()
        {

            // Mock project
            var disabledTestProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .WithProperty("pimsWriteSyncEnabled", false)
                .AddToMockService();

            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(disabledTestProject.Project);


            using var adminScope = fixture.AdminScope();

            var position = disabledTestProject.AddPosition()
                .WithInstances(s => s.AddInstance(DateTime.Today.Subtract(TimeSpan.FromDays(10)), TimeSpan.FromDays(30)))
                .WithAssignedPerson(testUser);
            var instance = position.Instances.First();


            var response = await Client.TestClientOptionsAsync($"/projects/{position.ProjectId}/positions/{position.Id}/instances/{instance.Id}/resources/requests?requestType=resourceOwnerChange");
            response.Should().BeSuccessfull();
            response.Should().NotHaveAllowHeaders(HttpMethod.Post);

            var error = JsonConvert.DeserializeAnonymousType(response.Content, new { error = new { code = string.Empty, message = string.Empty } });
            error!.error.code.Should().Be("ChangeRequestsDisabled");
        }

        [Fact]
        public async Task CheckChangeRequest_Should_HaveAllowedPost_When_PimsWriteSyncEnabled()
        {

            using var adminScope = fixture.AdminScope();

            var position = testProject.AddPosition()
                .WithInstances(s => s.AddInstance(DateTime.Today.Subtract(TimeSpan.FromDays(10)), TimeSpan.FromDays(30)))
                .WithAssignedPerson(testUser);
            var instance = position.Instances.First();


            var response = await Client.TestClientOptionsAsync($"/projects/{position.ProjectId}/positions/{position.Id}/instances/{instance.Id}/resources/requests?requestType=resourceOwnerChange");

            response.Should().BeSuccessfull();
            response.Should().HaveAllowHeaders(HttpMethod.Post);
        }

        [Fact]
        public async Task CheckChangeRequest_Should_BeEnabled_When_ChangeRequestEnabledFlagPresent()
        {

            // Mock project
            var disabledTestProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .WithProperty("pimsWriteSyncEnabled", false)
                .WithProperty("resourceOwnerRequestsEnabled", true)
                .AddToMockService();

            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(disabledTestProject.Project);


            using var adminScope = fixture.AdminScope();

            var position = disabledTestProject.AddPosition()
                .WithInstances(s => s.AddInstance(DateTime.Today.Subtract(TimeSpan.FromDays(10)), TimeSpan.FromDays(30)))
                .WithAssignedPerson(testUser);
            var instance = position.Instances.First();


            var response = await Client.TestClientOptionsAsync($"/projects/{position.ProjectId}/positions/{position.Id}/instances/{instance.Id}/resources/requests?requestType=resourceOwnerChange");
            response.Should().BeSuccessfull();
            response.Should().HaveAllowHeaders(HttpMethod.Post);
        }
    }

}