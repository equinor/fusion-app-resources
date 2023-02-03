using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Resources.Api.Tests.FusionMocks;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain;
using Fusion.Testing;
using Fusion.Testing.Mocks.LineOrgService;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.LineOrgService;
using Fusion.Testing.Mocks.OrgService;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Linq;

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

        [Theory]
        [InlineData("fulldepartment startswith 'PDP'", 2)]
        [InlineData("shortName contains 'CC'", 2)]
        [InlineData("department endswith 'CCM7'", 1)]
        [InlineData("sapId eq '52752459'", 1)]
        [InlineData("sapId neq '52752459'", 2)]
        [InlineData("name eq 'Construction %26 Commissioning'", 1)]
        public async Task ShouldReturnCorectCountGetRelevantDepartments_ShouldReturnCorrectCount(string filter, int count)
        {
            var assignedOrgUnit = new
            {
                name = "Const & Commissioning 7",
                sapId = "52827379",
                shortName = "CCM7",
                department = "FE CC CCM7",
                fullDepartment = "PDP PRD FE CC CCM7"

            };
            var delegatedOrgUnit = new
            {
                name = "Construction & Commissioning",
                sapId = "52752459",
                shortName = "CC",
                department = "PRD FE CC",
                fullDepartment = "PDP PRD FE CC"

            };
            var seconddelegatedOrgUnit = new
            {
                name = "Project Dev & Plant Main",
                sapId = "52525936",
                shortName = "PDP",
                department = "FOS FOIT PDP",
                fullDepartment = "TDI OG FOS FOIT PDP"
            };

            fixture.EnsureDepartment(assignedOrgUnit.fullDepartment);
            fixture.EnsureDepartment(delegatedOrgUnit.fullDepartment);
            fixture.EnsureDepartment(seconddelegatedOrgUnit.fullDepartment);
            testUser.IsResourceOwner = true;

            testUser.Roles = new List<ApiPersonRoleV3>
            {
                new ApiPersonRoleV3
                {
                    Name = AccessRoles.ResourceOwner,
                    Scope = new ApiPersonRoleScopeV3 { Type = "OrgUnit", Value = delegatedOrgUnit.fullDepartment },
                    ActiveToUtc = DateTime.UtcNow.AddDays(1),
                    IsActive = true,
                },
                new ApiPersonRoleV3
                {
                    Name = AccessRoles.ResourceOwner,
                    Scope = new ApiPersonRoleScopeV3 { Type = "OrgUnit", Value = seconddelegatedOrgUnit.fullDepartment },
                    ActiveToUtc = DateTime.UtcNow.AddDays(1),
                    IsActive = true,
                },
            };




            LineOrgServiceMock.AddOrgUnit(assignedOrgUnit.sapId, assignedOrgUnit.name, assignedOrgUnit.department, assignedOrgUnit.fullDepartment, assignedOrgUnit.shortName);
            LineOrgServiceMock.AddOrgUnit(delegatedOrgUnit.sapId, delegatedOrgUnit.name, delegatedOrgUnit.department, delegatedOrgUnit.fullDepartment, delegatedOrgUnit.shortName);
            LineOrgServiceMock.AddOrgUnit(seconddelegatedOrgUnit.sapId, seconddelegatedOrgUnit.name, seconddelegatedOrgUnit.department, seconddelegatedOrgUnit.fullDepartment, seconddelegatedOrgUnit.shortName);



            using (var adminScope = fixture.AdminScope())
            {
                var client = fixture.ApiFactory.CreateClient();
                await client.AddDelegatedDepartmentOwner(testUser, delegatedOrgUnit.fullDepartment, DateTime.Now.AddDays(-7), DateTime.Now.AddDays(7));
                await client.AddDelegatedDepartmentOwner(testUser, seconddelegatedOrgUnit.fullDepartment, DateTime.Now.AddDays(-7), DateTime.Now.AddDays(7));
            }

            using (var userScope = fixture.UserScope(testUser))
            {
                testUser.FullDepartment = assignedOrgUnit.fullDepartment;
                var client = fixture.ApiFactory.CreateClient();
                var resp = await client.TestClientGetAsync<ApiCollection<ApiRelevantOrgUnitTestModel>>(
                    $"/persons/{testUser.AzureUniqueId}/resources/relevant-departments?$filter={filter}"

                );

                resp.Should().BeSuccessfull();
                resp.Value.Value.Count().Should().Be(count);

            }
        }


        public class ApiRelevantOrgUnitTestModel
        {
            public string SapId { get; set; }
            public string FullDepartment { get; set; }
            public List<string> Reasons { get; set; } = new();
            public string Name { get; set; }
            public string ParentSapId { get; set; }
            public string ShortName { get; set; }
            public string Department { get; set; }


        }
    }
}