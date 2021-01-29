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

        private Guid TestResponsibilitMatrixId;

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
        public async Task ListAbsence_ShouldBeOk_WhenAdmin()
        {
            using var adminScope = fixture.AdminScope();
            var response = await client.TestClientGetAsync($"/persons/{testUser.AzureUniqueId}/absence", new { value = new[] { new { id = Guid.Empty } } });
            response.Should().BeSuccessfull();

            response.Value.value.Count().Should().BeGreaterOrEqualTo(1);
        }

        [Fact]
        public async Task GetAbsence_ShouldBeOk_WhenAdmin()
        {
            using var authScope = fixture.AdminScope();
            var response = await client.TestClientGetAsync<TestResponsibilitMatrix>($"/persons/{testUser.AzureUniqueId}/absence/{TestResponsibilitMatrixId}");
            response.Should().BeSuccessfull();

            response.Value.Id.Should().NotBeEmpty();
        }

        [Fact]
        public async Task PutAbsence_ShouldBeOk_WhenAdmin()
        {
            var request = new CreateResponsibilityMatrixRequest
            {

            };

            using var authScope = fixture.AdminScope();
            var response = await client.TestClientPutAsync<TestResponsibilitMatrix>($"/persons/{testUser.AzureUniqueId}/absence/{TestResponsibilitMatrixId}", request);
            response.Should().BeSuccessfull();

            response.Value.Id.Should().NotBeEmpty();
        }

        [Fact]
        public async Task DeleteAbsence_ShouldBeOk_WhenAdmin()
        {
            using var authScope = fixture.AdminScope();
            var response = await client.TestClientDeleteAsync($"/persons/{testUser.AzureUniqueId}/absence/{TestResponsibilitMatrixId}");
            response.Should().BeSuccessfull();
        }


        public async Task InitializeAsync()
        {
            var client = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            var request = new CreateResponsibilityMatrixRequest
            {

            };

            var response = await client.TestClientPostAsync<TestResponsibilitMatrix>($"/persons/{testUser.AzureUniqueId}/absence", request);

            TestResponsibilitMatrixId = response.Value.Id;
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
        public string? Discipline { get; set; }
    }
}
