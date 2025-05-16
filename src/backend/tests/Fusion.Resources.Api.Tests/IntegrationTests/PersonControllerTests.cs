using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Resources.Api.Tests.Helpers.Models.Responses;
using Fusion.Resources.Domain;
using Fusion.Testing;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.LineOrgService;
using Fusion.Testing.Mocks.OrgService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion.Resources.Api.Tests.FusionMocks;
using Fusion.Testing.Mocks.ProfileService;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    [Collection("Integration")]
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
            fixture.DisableMemoryCache();
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
        public async Task GetProfile_ShouldReturnValidDelegatedResponsibility()
        {
            var source = $"Department.Test";
            var delegatedDepartment = "AAA BBB CCC DDD";
            var secondDelegatedDepartment = "AAA BBB CCC EEE";
            var expiredDelegatedDepartment = "AAA BBB CCC FFF";
            var mainResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            mainResourceOwner.FullDepartment = $"AAA BBB CCC DDD EE FFF";



            LineOrgServiceMock.AddTestUser().MergeWithProfile(mainResourceOwner).AsResourceOwner().WithFullDepartment(delegatedDepartment).SaveProfile();
            using var adminScope = fixture.AdminScope();

            fixture.EnsureDepartment(delegatedDepartment, null, mainResourceOwner);
            fixture.EnsureDepartment(secondDelegatedDepartment, null, mainResourceOwner);
            fixture.EnsureDepartment(expiredDelegatedDepartment, null, mainResourceOwner, -2, -1);

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
            resp.Value.responsibilityInDepartments.Should().Contain(d => d.Equals(secondDelegatedDepartment));
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
        public async Task GetRelevantDepartmentsV10_ShouldReturnCorrectCount(string filter, int count)
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
                await client.AddDelegatedDepartmentOwner(testUser, delegatedOrgUnit.fullDepartment, DateTime.Now.AddHours(-1), DateTime.Now.AddDays(7));
                await client.AddDelegatedDepartmentOwner(testUser, seconddelegatedOrgUnit.fullDepartment, DateTime.Now.AddHours(-1), DateTime.Now.AddDays(7));
            }

            using (var userScope = fixture.UserScope(testUser))
            {
                testUser.FullDepartment = assignedOrgUnit.fullDepartment;
                var client = fixture.ApiFactory.CreateClient();
                var resp = await client.TestClientGetAsync<ApiCollection<TestApiRelevantOrgUnitModel>>(
                    $"/persons/{testUser.AzureUniqueId}/resources/relevant-departments?$filter={filter}"

                );

                resp.Should().BeSuccessfull();
                resp.Value.Value.Count().Should().Be(count);
            }
        }

        [Fact]
        public async Task GetRelevantDepartmentsV10_ShouldReturn_AssignedOrgUnit()
        {
            var assignedOrgUnit = LineOrgServiceMock.AddOrgUnit(
                sapId: "7228321",
                name: "bla bla",
                department: "KAGA PRD",
                fullDepartment: "KAGA PRD",
                shortname: "PRD"
            );

            var manager = fixture.AddProfile(s =>
            {
                s.WithAccountType(FusionAccountType.Employee);
                s.WithFullDepartment("KAGA"); // Is in parent department, but is manager of child department
            });
            LineOrgServiceMock.AddOrgUnitManager(assignedOrgUnit.FullDepartment, manager);

            using (var userScope = fixture.UserScope(manager))
            {
                var client = fixture.ApiFactory.CreateClient();
                var resp = await client.TestClientGetAsync<ApiCollection<TestApiRelevantOrgUnitModel>>(
                    $"/persons/{manager.AzureUniqueId}/resources/relevant-departments?api-version=1.0"
                );

                resp.Should().BeSuccessfull();
                resp.Value.Value.Should().OnlyContain(d => d.SapId == assignedOrgUnit.SapId && d.Reasons.Count != 0,
                    "Assigned org unit should be returned with reasons");
            }
        }

        [Theory]
        [InlineData("ResourceOwner")]
        [InlineData("DelegatedResourceOwner")]
        public async Task GetRelevantDepartmentsV11_ReasonsShouldBePopulatedFor_Assigned_Children_SiblingOrgUnits(string role)
        {
            var assignedOrgUnit = LineOrgServiceMock.AddOrgUnit(
                sapId: "7528321",
                name: "bla bla",
                department: "KRA PRD",
                fullDepartment: "KRA PRD",
                shortname: "PRD"
            );

            var childOrgUnit = LineOrgServiceMock.AddOrgUnit(
                sapId: "7528322",
                name: "bla bla bla",
                department: "KRA PRD FE",
                fullDepartment: "KRA PRD FE",
                shortname: "FE"
            );

            var siblingOrgUnit1 = LineOrgServiceMock.AddOrgUnit(
                sapId: "7528323",
                name: "bla bla bla",
                department: "KRA OIS",
                fullDepartment: "KRA OIS",
                shortname: "OIS"
            );

            var siblingOrgUnit2 = LineOrgServiceMock.AddOrgUnit(
                sapId: "7528324",
                name: "bla bla bla",
                department: "KRA AIS",
                fullDepartment: "KRA AIS",
                shortname: "AIS"
            );

            // Irrelevant org units
            {
                LineOrgServiceMock.AddOrgUnit(
                    sapId: "7528325",
                    name: "Irrelevant: Child of sibling",
                    department: "KRA OIS FE2",
                    fullDepartment: "KRA OIS FE2",
                    shortname: "OIS"
                );

                LineOrgServiceMock.AddOrgUnit(
                    sapId: "7522322",
                    name: "Irrelevant: Child of child",
                    department: "PRD FE CC",
                    fullDepartment: "KRA PRD FE CC",
                    shortname: "CC"
                );

                LineOrgServiceMock.AddOrgUnit(
                    sapId: "7522323",
                    name: "Irrelevant: Similar name,but different department",
                    department: "LOI PRD",
                    fullDepartment: "LOI PRD",
                    shortname: "PRD"
                );
            }


            var responsiblePerson = fixture.AddProfile(s =>
            {
                s.WithAccountType(FusionAccountType.Employee);
                s.WithFullDepartment("KRA"); // Is in parent department, but is manager of child department
            });

            switch (role)
            {
                case "ResourceOwner":
                    LineOrgServiceMock.AddOrgUnitManager(assignedOrgUnit.FullDepartment, responsiblePerson);
                    break;

                case "DelegatedResourceOwner":
                    PeopleServiceMock.AddRole(responsiblePerson.AzureUniqueId!.Value, new ApiPersonRoleV3
                    {
                        Name = AccessRoles.ResourceOwner,
                        Scope = new ApiPersonRoleScopeV3 { Type = "OrgUnit", Value = assignedOrgUnit.FullDepartment! },
                        SourceSystem = "Department.Test",
                        IsActive = true,
                        OnDemandSupport = false
                    });
                    break;

                default:
                    throw new InvalidOperationException($"Invalid test case '{role}' not supported");
            }

            using (fixture.UserScope(responsiblePerson))
            {
                var client = fixture.ApiFactory.CreateClient();
                var resp = await client.TestClientGetAsync<ApiCollection<TestApiRelevantOrgUnitModel>>(
                    $"/persons/{responsiblePerson.AzureUniqueId}/resources/relevant-departments?api-version=1.1"
                );

                resp.Should().BeSuccessfull();

                var relevantOrgUnits = resp.Value.Value
                    .Where(r => r.Reasons.Count != 0)
                    .ToList();

                relevantOrgUnits.Should().HaveCount(4, "Only 4 org units should be returned with reasons");
                relevantOrgUnits.Should().ContainSingle(d => d.SapId == assignedOrgUnit.SapId,
                    "Assigned org unit should be returned with reasons");
                relevantOrgUnits.Should().ContainSingle(d => d.SapId == childOrgUnit.SapId,
                    "Child org unit should be returned with reasons");
                relevantOrgUnits.Should().ContainSingle(d => d.SapId == siblingOrgUnit1.SapId,
                    "Sibling org unit 1 should be returned with reasons");
                relevantOrgUnits.Should().ContainSingle(d => d.SapId == siblingOrgUnit2.SapId,
                    "Sibling org unit 2 should be returned with reasons");
            }
        }
    }
}
