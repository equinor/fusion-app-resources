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
using Fusion.Resources.Domain;
using Fusion.Testing.Authentication.User;
using Xunit;
using Xunit.Abstractions;
#nullable enable
namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class PersonAbsenceTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;
        /// <summary>
        /// Will be generated new for each test
        /// </summary>
        private readonly ApiPersonProfileV3 testUser;

        private Guid TestAbsenceId;

        private HttpClient client => fixture.ApiFactory.CreateClient();

        public PersonAbsenceTests(ResourceApiFixture fixture, ITestOutputHelper output)
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
            var response = await client.TestClientGetAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence/{TestAbsenceId}");
            response.Should().BeSuccessfull();

            response.Value.Id.Should().NotBeEmpty();
            response.Value.AppliesFrom.Should().NotBeNull();
            response.Value.AppliesTo.Should().NotBeNull();
            response.Value.Comment.Should().NotBeNullOrEmpty();
            response.Value.Type.Should().NotBeNull();
            response.Value.AbsencePercentage.Should().NotBeNull();
        }

        [Fact]
        public async Task PutAbsence_ShouldBeOk_WhenAdmin()
        {
            var request = new CreatePersonAbsenceRequest
            {
                AppliesFrom = DateTimeOffset.UtcNow,
                AppliesTo = DateTimeOffset.UtcNow.AddYears(1),
                Comment = "A comment",
                Type = ApiPersonAbsence.ApiAbsenceType.Vacation,
                AbsencePercentage = null // Clearing absencePercentage = 100% 
            };

            using var authScope = fixture.AdminScope();
            var response = await client.TestClientPutAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence/{TestAbsenceId}", request);
            response.Should().BeSuccessfull();

            response.Value.Id.Should().Be(TestAbsenceId);
            response.Value.AppliesFrom.Should().Be(request.AppliesFrom);
            response.Value.AppliesTo.Should().Be(request.AppliesTo);
            response.Value.Comment.Should().Be(request.Comment);
            response.Value.Type.Should().Be(request.Type);
            response.Value.AbsencePercentage.Should().BeNull();

        }

        [Fact]
        public async Task DeleteAbsence_ShouldBeOk_WhenAdmin()
        {
            using var authScope = fixture.AdminScope();
            var response = await client.TestClientDeleteAsync($"/persons/{testUser.AzureUniqueId}/absence/{TestAbsenceId}");
            response.Should().BeSuccessfull();
        }


        public async Task InitializeAsync()
        {
            var client = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            var request = new CreatePersonAbsenceRequest
            {
                AppliesFrom = DateTimeOffset.UtcNow,
                AppliesTo = DateTimeOffset.UtcNow.AddYears(1),
                Comment = "A comment",
                Type = ApiPersonAbsence.ApiAbsenceType.Absence,
                AbsencePercentage = 100
            };

            var response = await client.TestClientPostAsync<TestAbsence>($"/persons/{testUser.AzureUniqueId}/absence", request);

            TestAbsenceId = response.Value.Id;
        }


        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }
    }

    public class TestAbsence
    {
        public Guid Id { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset? AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public QueryAbsenceType? Type { get; set; }
        public double? AbsencePercentage { get; set; }
    }
}
