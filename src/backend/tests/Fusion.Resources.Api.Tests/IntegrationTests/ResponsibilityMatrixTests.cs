using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Resources.Api.Controllers;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks.OrgService;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class ResponsibilityMatrixTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;
        /// <summary>
        /// Will be generated new for each test
        /// </summary>
        private readonly ApiPersonProfileV3 testUser;


        private Guid testResponsibilityMatrixId;

        private FusionTestProjectBuilder testProject;


        private HttpClient client => fixture.ApiFactory.CreateClient();

        public ResponsibilityMatrixTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.Employee);
        }

        [Fact]
        public async Task ListMatrix_ShouldBeOk_WhenAdmin()
        {
            using var adminScope = fixture.AdminScope();
            var response = await client.TestClientGetAsync($"/internal-resources/responsibility-matrix", new { value = new[] { new { id = Guid.Empty } } });
            response.Should().BeSuccessfull();

            response.Value.value.Count().Should().BeGreaterOrEqualTo(1);
        }

        [Fact]
        public async Task GetMatrix_ShouldBeOk_WhenAdmin()
        {
            using var authScope = fixture.AdminScope();
            var response = await client.TestClientGetAsync<TestResponsibilitMatrix>($"/internal-resources/responsibility-matrix/{testResponsibilityMatrixId}");
            response.Should().BeSuccessfull();

            response.Value.Id.Should().NotBeEmpty();
        }

        [Fact]
        public async Task PutMatrix_ShouldBeOk_WhenAdmin()
        {
            var request = new UpdateResponsibilityMatrixRequest
            {
                ProjectId = testProject.Project.ProjectId,
                LocationId = Guid.NewGuid(),
                Discipline = "WallaWallaUpdated",
                BasePositionId = testProject.Positions.First().BasePosition.Id,
                Sector = "ABC DEF",
                Unit = "ABC DEF GHI",
                ResponsibleId = testUser.AzureUniqueId.GetValueOrDefault()
            };

            using var authScope = fixture.AdminScope();
            var response = await client.TestClientPutAsync<TestResponsibilitMatrix>($"/internal-resources/responsibility-matrix/{testResponsibilityMatrixId}", request);
            response.Should().BeSuccessfull();

            response.Value.Id.Should().NotBeEmpty();
            response.Value.Sector.Should().Be(request.Sector);
            response.Value.Unit.Should().Be(request.Unit);
        }

        [Fact]
        public async Task DeleteMatrix_ShouldBeOk_WhenAdmin()
        {
            using var authScope = fixture.AdminScope();
            var response = await client.TestClientDeleteAsync($"/internal-resources/responsibility-matrix/{testResponsibilityMatrixId}");
            response.Should().BeSuccessfull();
        }


        public async Task InitializeAsync()
        {
            testProject = new FusionTestProjectBuilder()
                .WithPositions()
                .AddToMockService();

            fixture.ContextResolver
                .AddContext(testProject.Project);


            var client = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            var request = new CreateResponsibilityMatrixRequest
            {
                ProjectId = testProject.Project.ProjectId,
                LocationId = Guid.NewGuid(),
                Discipline = "WallaWalla",
                BasePositionId = testProject.Positions.First().BasePosition.Id,
                Sector = "ABC",
                Unit = "ABC DEF",
                ResponsibleId = testUser.AzureUniqueId.GetValueOrDefault()
            };

            var response = await client.TestClientPostAsync<TestResponsibilitMatrix>($"/internal-resources/responsibility-matrix", request);
            response.Response.IsSuccessStatusCode.Should().BeTrue();
            testResponsibilityMatrixId = response.Value.Id;
        }


        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }
    }

    public class TestResponsibilitMatrix
    {
        public Guid Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public object CreatedBy { get; set; } = null!;
        public object Project { get; set; } = null!;
        public object Location { get; set; }
        public string? Discipline { get; set; }
        public object BasePosition { get; set; }
        public string? Sector { get; set; }
        public string? Unit { get; set; }
        public object Responsible { get; set; } = null!;
    }
}
