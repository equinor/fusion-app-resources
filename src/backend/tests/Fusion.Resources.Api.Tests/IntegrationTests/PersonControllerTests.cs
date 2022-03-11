using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Mocks.OrgService;
using System;
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
        private FusionTestProjectBuilder testProject;

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
            var actualDept = "TPD PRD TST ABC";
            var currentDelegatedDept = "TPD PRD TST ASD QWE";
            var futureDelegatedDept = "TPD PRD TST FUT WFD";
            var previousDelegatedDept = "TPD PRD TST PRV WFD";

            fixture.EnsureDepartment(actualDept);
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
                testUser.FullDepartment = actualDept;
                var client = fixture.ApiFactory.CreateClient();
                var resp = await client.TestClientGetAsync(
                    $"/persons/me/resources/profile",
                    new { responsibilityInDepartments = Array.Empty<string>() }
                );

                resp.Should().BeSuccessfull();
                resp.Value.responsibilityInDepartments.Should().Contain(currentDelegatedDept);
                resp.Value.responsibilityInDepartments.Should().NotContain(futureDelegatedDept);
                resp.Value.responsibilityInDepartments.Should().NotContain(previousDelegatedDept);
            }
        }

        [Fact]
        public async Task GetProfile_ShouldBeEmpty_WhenUserHasNoDepartment()
        {
            using (var userScope = fixture.UserScope(testUser))
            {
                testUser.FullDepartment = null;
                var client = fixture.ApiFactory.CreateClient();
                var resp = await client.TestClientGetAsync(
                    $"/persons/me/resources/profile",
                    new
                    {
                        fullDepartment = default(string),
                        isResourceOwner = true,
                        responsibilityInDepartments = Array.Empty<string>()
                    }
                );

                resp.Should().BeSuccessfull();
                resp.Value.fullDepartment.Should().BeNull();
                resp.Value.isResourceOwner.Should().BeFalse();
            }
        }


        [Fact]
        public async Task GetProfile_ShouldBeNotFound_WhenUserDoesNotExist()
        {
            using var userScope = fixture.AdminScope();
            var client = fixture.ApiFactory.CreateClient();
            var resp = await client.TestClientGetAsync(
                $"/persons/{Guid.NewGuid()}/resources/profile",
                new
                {
                    fullDepartment = default(string),
                    isResourceOwner = true,
                    responsibilityInDepartments = Array.Empty<string>()
                }
            );

            resp.Should().BeNotFound();
        }

        [Fact]
        public async Task AllocationRequestStatus_ShouldBeAutoApproval_WhenAccountTypeIsContractor()
        {
            var testUser = fixture.AddProfile(FusionAccountType.Consultant);

            using var userScope = fixture.AdminScope();
            
            var client = fixture.ApiFactory.CreateClient();
            var resp = await client.TestClientGetAsync($"/persons/{testUser.AzureUniqueId}/resources/allocation-request-status",
                new
                {
                    autoApproval = false
                }
            );

            resp.Should().BeSuccessfull();
            resp.Value.autoApproval.Should().BeTrue();
        }

        [Fact]
        public async Task AllocationRequestStatus_ShouldIncludeManager()
        {
            var manager = fixture.AddProfile(FusionAccountType.Employee);
            var testUser = fixture.AddProfile(s => s
                .WithAccountType(FusionAccountType.Employee)
                .WithManager(manager));

            using var userScope = fixture.AdminScope();

            var client = fixture.ApiFactory.CreateClient();
            var resp = await client.TestClientGetAsync($"/persons/{testUser.AzureUniqueId}/resources/allocation-request-status",
                new
                {
                    manager = new
                    {
                        azureUniquePersonId = Guid.Empty
                    }

                }
            );

            resp.Should().BeSuccessfull();
            resp.Value.manager.Should().NotBeNull();
            resp.Value.manager.azureUniquePersonId.Should().Be(manager.AzureUniqueId.Value);
        }

        [Fact]
        public async Task AllocationRequestStatus_ShouldBeUnauthorized_WhenExternal()
        {
            var testUser = fixture.AddProfile(FusionAccountType.Consultant);
            var externalUser = fixture.AddProfile(FusionAccountType.External);

            using var userScope = fixture.UserScope(externalUser);

            var client = fixture.ApiFactory.CreateClient();
            var resp = await client.TestClientGetAsync($"/persons/{testUser.AzureUniqueId}/resources/allocation-request-status",
                new { autoApproval = false }
            );

            resp.Should().BeUnauthorized();
        }

        public Task InitializeAsync() => Task.CompletedTask;
        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }
    }
}
