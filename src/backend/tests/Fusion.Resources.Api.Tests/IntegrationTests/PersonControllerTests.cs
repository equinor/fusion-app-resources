using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Mocks.OrgService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class PersonControllerTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;
        /// <summary>
        /// Will be generated new for each test
        /// </summary>
        private readonly ApiPersonProfileV3 testUser;


        private Guid testResponsibilityMatrixId;

        private FusionTestProjectBuilder testProject;


        private HttpClient Client => fixture.ApiFactory.CreateClient();

        public PersonControllerTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.Employee);
            
            testProject = new FusionTestProjectBuilder()
                .WithPositions()
                .AddToMockService();

            fixture.ContextResolver
                .AddContext(testProject.Project);
        }

        [Fact]
        public async Task ShouldIgnoreNonCurrentDepartmentDelegations()
        {
            var currentDelegatedDept = "TPD PRD TST ASD QWE";
            var futureDelegatedDept = "TPD PRD TST FUT WFD";
            var previousDelegatedDept = "TPD PRD TST PRV WFD";

            fixture.EnsureDepartment(currentDelegatedDept);
            fixture.EnsureDepartment(futureDelegatedDept);
            fixture.EnsureDepartment(previousDelegatedDept);


            using (var adminScope = fixture.AdminScope())
            {
                var client = fixture.ApiFactory.CreateClient();
                await client.AddDelegatedDepartmentOwner(testUser, currentDelegatedDept, DateTime.Now.AddDays(-7), DateTime.Now.AddDays(7));
                await client.AddDelegatedDepartmentOwner(testUser, futureDelegatedDept, DateTime.Now.AddDays(7), DateTime.Now.AddDays(14));
                await client.AddDelegatedDepartmentOwner(testUser, previousDelegatedDept, DateTime.Now.AddDays(-14), DateTime.Now.AddDays(-7));
            }

            using (var userScope = fixture.UserScope(testUser))
            {
                var client = fixture.ApiFactory.CreateClient();
                var resp = await client.TestClientGetAsync(
                    $"/persons/me/resources/profile?api-version=1.0-preview",
                    new { responsibilityInDepartments = Array.Empty<string>() }
                );

                resp.Should().BeSuccessfull();
                resp.Value.responsibilityInDepartments.Should().Contain(currentDelegatedDept);
                resp.Value.responsibilityInDepartments.Should().NotContain(futureDelegatedDept);
                resp.Value.responsibilityInDepartments.Should().NotContain(previousDelegatedDept);
            }
        }



        public Task InitializeAsync() => Task.CompletedTask;
        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }
    }
}
