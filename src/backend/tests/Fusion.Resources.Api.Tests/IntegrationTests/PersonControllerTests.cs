using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Resources.Api.Tests.FusionMocks;
using Fusion.Resources.Domain;
using Fusion.Testing;
using Fusion.Testing.Mocks.LineOrgService;
using Fusion.Testing.Mocks.OrgService;
using System;
using System.Linq;
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
        public async Task GetProfile_ShouldReturnValidDelegatedResposibility()
        {
            var source = $"Department.Test";
            var delegatedDepartment = "AAA BBB CCC DDD";
            var seconddelegateddDepartment = "AAA BBB CCC EEE";
            var expireddelegateddDepartment = "AAA BBB CCC FFF";
            var notStarteddelegateddDepartment = "AAA BBB CCC GGG";
            var mainResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            mainResourceOwner.FullDepartment = $"AAA BBB CCC DDD EE FFF";



            LineOrgServiceMock.AddTestUser().MergeWithProfile(mainResourceOwner).AsResourceOwner().WithFullDepartment(delegatedDepartment).SaveProfile();
            using var adminScope = fixture.AdminScope();

            fixture.EnsureDepartment(delegatedDepartment, null, mainResourceOwner);
            fixture.EnsureDepartment(seconddelegateddDepartment, null, mainResourceOwner);
            fixture.EnsureDepartment(expireddelegateddDepartment, null, mainResourceOwner, -2, -1);
            fixture.EnsureDepartment(notStarteddelegateddDepartment, null, mainResourceOwner, +2, +5);

            var manager = fixture.AddProfile(FusionAccountType.Employee);
            var testUser = fixture.AddProfile(s => s
                .WithAccountType(FusionAccountType.Employee)
                .WithManager(manager));

      
                var client = fixture.ApiFactory.CreateClient();
                var resp = await client.TestClientGetAsync(
                    $"/persons/{mainResourceOwner.AzureUniqueId}/resources/profile",
                    new
                    {
                        fullDepartment = default(string),
                        isResourceOwner = true,
                        responsibilityInDepartments = Array.Empty<string>()
                    }
                );

            resp.Should().BeSuccessfull();
            resp.Value.responsibilityInDepartments.Count().Should().Be(2);
            resp.Value.responsibilityInDepartments.Should().Contain(d => d.Equals(delegatedDepartment));
            resp.Value.responsibilityInDepartments.Should().Contain(d => d.Equals(seconddelegateddDepartment));



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