﻿using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Resources.Api.Tests.FusionMocks;
using Fusion.Resources.Domain;
using Fusion.Testing;
using Fusion.Testing.Mocks.LineOrgService;
using Fusion.Testing.Mocks.OrgService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Services.LineOrg.ApiModels;
using Fusion.Testing.Mocks.ProfileService;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    [Collection("Integration")]
    public class DepartmentsControllerTests : IClassFixture<ResourceApiFixture>
    {
        private ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private ApiPersonProfileV3 testUser;

        public DepartmentsControllerTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.External);
            fixture.DisableMemoryCache();
        }

        private HttpClient Client => fixture.ApiFactory.CreateClient();

        [Fact]
        public async Task GetDepartment_ShouldGiveNotFound_WhenNotInLineOrg()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync<TestDepartment>("/departments/NOT EXI ST ING");

            resp.Response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Theory]
        [InlineData("ResourceOwner")]
        [InlineData("DelegatedResourceOwner")]
        public async Task GetDepartmentV11_BeOk_ForRelevantDepartments(string role)
        {
            var assignedOrgUnit = LineOrgServiceMock.AddOrgUnit(
                sapId: "75283211",
                name: "bla bla",
                department: "LKA PRD",
                fullDepartment: "LKA PRD",
                shortname: "PRD"
            );

            var childOrgUnit = LineOrgServiceMock.AddOrgUnit(
                sapId: "75283221",
                name: "bla bla bla",
                department: "LKA PRD FE",
                fullDepartment: "LKA PRD FE",
                shortname: "FE"
            );

            var siblingOrgUnit1 = LineOrgServiceMock.AddOrgUnit(
                sapId: "75283231",
                name: "bla bla bla",
                department: "LKA OIS",
                fullDepartment: "LKA OIS",
                shortname: "OIS"
            );

            var siblingOrgUnit2 = LineOrgServiceMock.AddOrgUnit(
                sapId: "75283241",
                name: "bla bla bla",
                department: "LKA AIS",
                fullDepartment: "LKA AIS",
                shortname: "AIS"
            );

            // Irrelevant org units
            {
                LineOrgServiceMock.AddOrgUnit(
                    sapId: "75283251",
                    name: "Irrelevant: Child of sibling",
                    department: "LKA OIS FE2",
                    fullDepartment: "LKA OIS FE2",
                    shortname: "OIS"
                );

                LineOrgServiceMock.AddOrgUnit(
                    sapId: "7522322",
                    name: "Irrelevant: Child of child",
                    department: "PRD FE CC",
                    fullDepartment: "LKA PRD FE CC",
                    shortname: "CC"
                );

                LineOrgServiceMock.AddOrgUnit(
                    sapId: "75223231",
                    name: "Irrelevant: Similar name,but different department",
                    department: "LOI PRD",
                    fullDepartment: "LOI PRD",
                    shortname: "PRD"
                );
            }


            var responsiblePerson = fixture.AddProfile(s =>
            {
                s.WithAccountType(FusionAccountType.Employee);
                s.WithFullDepartment("LKA"); // Is in parent department, but is manager of child department
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

            var relevant = new List<ApiOrgUnit>
            {
                assignedOrgUnit,
                childOrgUnit,
                siblingOrgUnit1,
                siblingOrgUnit2
            };

            var irrelevantSapIds = new List<string>
            {
                "75283251",
                "7522322",
                "75223231"
            };

            using (fixture.UserScope(responsiblePerson))
            {
                foreach (var apiOrgUnit in relevant)
                {
                    var resp = await Client.TestClientGetAsync<TestDepartment>($"/departments/{apiOrgUnit.SapId}?api-version=1.1");
                    resp.Should().BeSuccessfull();
                }

                foreach (var sapId in irrelevantSapIds)
                {
                    var resp = await Client.TestClientGetAsync<TestDepartment>($"/departments/{sapId}?api-version=1.1");
                    resp.Response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
                }
            }
        }

        [Fact]
        public async Task GetDepartmentV11_Should_Forbid_Level2_To_Level1_Department()
        {
            var assignedOrgUnit = LineOrgServiceMock.AddOrgUnit(
                sapId: "752832112",
                name: "bla",
                department: "CAI PDD",
                fullDepartment: "CAI PDD",
                shortname: "PDD"
            );

            var siblingOrgUnit = LineOrgServiceMock.AddOrgUnit(
                sapId: "752832221",
                name: "bli",
                department: "LKA",
                fullDepartment: "LKA",
                shortname: "LKA"
            );

            var responsiblePerson = fixture.AddProfile(s =>
            {
                s.WithAccountType(FusionAccountType.Employee);
                s.WithFullDepartment("CORP"); // Is in the new workday CORP department
            });

            LineOrgServiceMock.AddOrgUnitManager(assignedOrgUnit.FullDepartment, responsiblePerson);

            using var userScope = fixture.UserScope(responsiblePerson);
            var resp = await Client.TestClientGetAsync<TestDepartment>($"/departments/{siblingOrgUnit.SapId}?api-version=1.1");
            resp.Response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetDepartment_Should_GetFromLineOrg_WhenNotInDb()
        {
            var department = "NOT IN DB";
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            LineOrgServiceMock.AddTestUser().MergeWithProfile(fakeResourceOwner).AsResourceOwner().WithFullDepartment(department).SaveProfile();

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync<TestDepartment>($"/departments/{department}");

            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Value.Name.Should().Be(department);
        }

        [Fact]
        public async Task SearchDepartment_Should_GetFromLineOrg_WhenNotInDb()
        {
            var department = "TPD LIN ORG TST1";
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            LineOrgServiceMock.AddTestUser().MergeWithProfile(fakeResourceOwner).AsResourceOwner().WithFullDepartment(department).SaveProfile();

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments?$search={fakeResourceOwner.Name}");

            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Value.Should().Contain(x => x.Name == department);
        }

        [Fact]
        public async Task SearchDepartment_Should_GetDelegatedResponsibles_FromRoleService()
        {
            var delegatedDepartment = "AAA BBB CCC";
            var nonDelegatedDepartment = "DDD EEE FFF";
            var mainResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var delegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var nonDelegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            await RolesClientMock.AddPersonRole(delegatedResourceOwner.AzureUniqueId!.Value, new Fusion.Integration.Roles.RoleAssignment
            {
                Identifier = $"{Guid.NewGuid()}",
                Scope = new Fusion.Integration.Roles.RoleAssignment.RoleScope("OrgUnit", delegatedDepartment),
                ValidTo = DateTime.UtcNow.AddDays(1),
                Source = "Department.Test"
            });

            await RolesClientMock.AddPersonRole(nonDelegatedResourceOwner.AzureUniqueId!.Value, new Fusion.Integration.Roles.RoleAssignment
            {
                Identifier = $"{Guid.NewGuid()}",
                RoleName = AccessRoles.ResourceOwner,
                Scope = new Fusion.Integration.Roles.RoleAssignment.RoleScope("OrgUnit", nonDelegatedDepartment),
                ValidTo = DateTime.UtcNow.AddDays(1),
                Source = "Department.Test"
            });

            LineOrgServiceMock.AddTestUser().MergeWithProfile(mainResourceOwner).AsResourceOwner().WithFullDepartment(delegatedDepartment).SaveProfile();
            fixture.EnsureDepartment(delegatedDepartment, null, delegatedResourceOwner);

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments?$search={mainResourceOwner.Name}");
            TestLogger.TryLogObject(resp);
            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);

            resp.Value.Should().Contain(x => x.Name == delegatedDepartment && x.DelegatedResponsibles.Any(y => y.AzureUniquePersonId.Equals(delegatedResourceOwner.AzureUniqueId)));
        }

        [Fact]
        public async Task ListDepartments_Should_PopulateSapId()
        {
            var testOrgUnit = LineOrgServiceMock.AddOrgUnit("MY TEST UNIT");

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/departments?$search=MY TEST", new[] { new { sapId = string.Empty } });
            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);

            resp.Value.Should().Contain(i => i.sapId.EqualsIgnCase(testOrgUnit.SapId));
        }

        [Fact]
        public async Task GetDepartment_Should_PopulateSapId_WhenDepartmentStringProvided()
        {
            var testOrgUnit = LineOrgServiceMock.AddOrgUnit("MY TEST UNIT 2");

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/departments/{testOrgUnit.FullDepartment}", new { sapId = string.Empty });
            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);

            resp.Value.sapId.Should().Be(testOrgUnit.SapId);
        }

        [Fact]
        public async Task GetDepartment_Should_ReturnOrgUnit_WhenUsingSapId()
        {
            var testOrgUnit = LineOrgServiceMock.AddOrgUnit("MY TEST UNIT 3");

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/departments/{testOrgUnit.SapId}", new { sapId = string.Empty });
            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);

            resp.Value.sapId.Should().Be(testOrgUnit.SapId);
        }

        [Fact]
        public async Task GetDepartment_Should_ReturnOrgUnit_WhenUsingFullDepartment()
        {
            var testOrgUnit = LineOrgServiceMock.AddOrgUnit("MY TEST UNIT 4");

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync($"/departments/{testOrgUnit.FullDepartment}", new { sapId = string.Empty });
            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);

            resp.Value.sapId.Should().Be(testOrgUnit.SapId);
        }

        [Fact]
        public async Task GetDepartment_Should_GetDelegatedResponsibles_FromGetDepartmentString()
        {
            var source = $"Department.Test";
            var delegatedDepartment = "AAA BBB CCC DDD";
            var mainResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            mainResourceOwner.FullDepartment = $"AAA BBB CCC DDD EE FFF";
            var delegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var secondDelegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var expiredDelegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var notStartedDelegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            LineOrgServiceMock.AddTestUser().MergeWithProfile(mainResourceOwner).AsResourceOwner().WithFullDepartment(delegatedDepartment).SaveProfile();
            using var adminScope = fixture.AdminScope();

            fixture.EnsureDepartment(delegatedDepartment, null, delegatedResourceOwner);
            fixture.EnsureDepartment(delegatedDepartment, null, secondDelegatedResourceOwner);
            fixture.EnsureDepartment(delegatedDepartment, null, expiredDelegatedResourceOwner, -2, -1);
            fixture.EnsureDepartment(delegatedDepartment, null, notStartedDelegatedResourceOwner, +2, +5);

            var resp = await Client.TestClientGetAsync<TestDepartment>($"/departments/{delegatedDepartment}");
            TestLogger.TryLogObject(resp);
            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);

            resp.Value.Name.Should().Contain(delegatedDepartment);
            resp.Value.DelegatedResponsibles.Should().HaveCount(2);
            resp.Value.DelegatedResponsibles.Should().Contain(d => d.AzureUniquePersonId.Equals(delegatedResourceOwner.AzureUniqueId));
            resp.Value.DelegatedResponsibles.Should().Contain(d => d.AzureUniquePersonId.Equals(secondDelegatedResourceOwner.AzureUniqueId));
        }

        [Fact]
        public async Task GetDepartments_Should_GetDelegatedResponsibles_FromGetDepartmentsQueryString()
        {
            var source = $"Department.Test";
            var delegatedDepartment = "BBB CCC DDD EEE";
            var mainResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            mainResourceOwner.FullDepartment = $"BBB CCC DDD EEE FFF GGG";
            var delegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var secondDelegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var expiredDelegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var notStartedDelegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);



            LineOrgServiceMock.AddTestUser().MergeWithProfile(mainResourceOwner).AsResourceOwner().WithFullDepartment(delegatedDepartment).SaveProfile();
            using var adminScope = fixture.AdminScope();

            fixture.EnsureDepartment(delegatedDepartment, null, delegatedResourceOwner);
            fixture.EnsureDepartment(delegatedDepartment, null, secondDelegatedResourceOwner);
            fixture.EnsureDepartment(delegatedDepartment, null, expiredDelegatedResourceOwner, -2, -1);
            fixture.EnsureDepartment(delegatedDepartment, null, notStartedDelegatedResourceOwner, +2, +5);


            var resp = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments?$search={mainResourceOwner.Name}");
            TestLogger.TryLogObject(resp);
            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);


            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Value.FirstOrDefault().DelegatedResponsibles.Count().Should().Be(2);
            resp.Value.FirstOrDefault().DelegatedResponsibles.Should().Contain(d => d.AzureUniquePersonId.Equals(delegatedResourceOwner.AzureUniqueId));
            resp.Value.FirstOrDefault().DelegatedResponsibles.Should().Contain(d => d.AzureUniquePersonId.Equals(secondDelegatedResourceOwner.AzureUniqueId));


        }

        [Fact]
        public async Task SearchShouldBeCaseInsensitive()
        {
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var orgUnit = fixture.SetAsResourceOwner(fakeResourceOwner, fakeResourceOwner.FullDepartment);


            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments?$search={fakeResourceOwner.Name.ToUpper()}");

            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Value.Should().Contain(x => x.Name == orgUnit.Name);
        }

        [Fact]
        public async Task OptionsDepartmentResponsible_ShouldBeDisallowed_WhenUser()
        {
            var testDepartment = "MY TPD LIN DEP1";
            fixture.EnsureDepartment(testDepartment);

            using var userScope = fixture.UserScope(testUser);
            var result = await Client.TestClientOptionsAsync($"/departments/{testDepartment}/delegated-resource-owners");
            result.Should().BeSuccessfull();
            result.CheckAllowHeader("OPTIONS, !DELETE, !POST, !GET");
        }
        [Fact]
        public async Task OptionsDepartmentResponsible_ShouldBeAllowed_WhenAdmin()
        {
            var testDepartment = "MY TPD LIN DEP2";
            fixture.EnsureDepartment(testDepartment);

            using var adminScope = fixture.AdminScope();
            var result = await Client.TestClientOptionsAsync($"/departments/{testDepartment}/delegated-resource-owners");
            result.Should().BeSuccessfull();
            result.CheckAllowHeader("OPTIONS, DELETE, POST, GET");
        }

        [Theory]
        [InlineData("AAA BBB", false)]
        [InlineData("AAA BBB CCC", false)]
        [InlineData("AAA BBB CCC YYY", true)]
        [InlineData("AAA BBB CCC XXX", true)] //<- ResourceOwner for this department
        [InlineData("AAA BBB CCC XXX EEE", true)]
        [InlineData("AAA BBB CCC XXX EEE FFF", false)]
        public async Task OptionsDepartmentResponsible_CanDelegateAccessToCurrentAndSiblingsAndDirectChildren_WhenResourceOwner(string fullDepartment, bool expectingAccess)
        {
            var departmentsToTest = new List<string> { "AAA BBB", "AAA BBB CCC", "AAA BBB CCC XXX", "AAA BBB CCC XXX EEE", "AAA BBB CCC XXX EEE FFF", "AAA BBB CCC YYY" };
            foreach (var dep in departmentsToTest)
                fixture.EnsureDepartment(dep);

            var testDepartment = "AAA BBB CCC XXX";
            var resourceOwner = fixture.AddResourceOwner(testDepartment); // AddProfile(x => x.WithAccountType(FusionAccountType.Employee).AsResourceOwner().WithFullDepartment(testDepartment));
            using var adminScope = fixture.UserScope(resourceOwner);

            var result = await Client.TestClientOptionsAsync($"/departments/{fullDepartment}/delegated-resource-owners");
            result.Should().BeSuccessfull();
            result.CheckAllowHeader(expectingAccess ? "OPTIONS, DELETE, POST, GET" : "OPTIONS, !DELETE, !POST, !GET");
        }

        [Fact]
        public async Task OptionsDepartmentResponsible_CanDelegateAccess_WhenResourceOwnerForMultiple()
        {
            var firstDepartment = "AAA BBB CCC";
            var secondDepartment = "XXX YYY ZZZ";
            fixture.EnsureDepartment(firstDepartment);
            fixture.EnsureDepartment(secondDepartment);

            var resourceOwner = fixture.AddResourceOwner(firstDepartment);
            fixture.SetAsResourceOwner(resourceOwner, secondDepartment);
            using var adminScope = fixture.UserScope(resourceOwner);

            var firstResult = await Client.TestClientOptionsAsync($"/departments/{firstDepartment}/delegated-resource-owners");
            firstResult.Should().BeSuccessfull();
            firstResult.CheckAllowHeader("OPTIONS, DELETE, POST, GET");

            var secondResult = await Client.TestClientOptionsAsync($"/departments/{secondDepartment}/delegated-resource-owners");
            secondResult.Should().BeSuccessfull();
            secondResult.CheckAllowHeader("OPTIONS, DELETE, POST, GET");
        }

        [Theory]
        [InlineData("AAA BBB", false)]
        [InlineData("AAA BBB CCC", false)]
        [InlineData("AAA BBB CCC YYY", true)]
        [InlineData("AAA BBB CCC XXX", true)] //<- ResourceOwner for this department
        [InlineData("AAA BBB CCC XXX EEE", true)]
        [InlineData("AAA BBB CCC XXX EEE FFF", false)]
        public async Task PostDepartmentResponsible_CanDelegateAccessToCurrentAndSiblingsAndDirectChildren_WhenResourceOwner(string fullDepartment, bool expectingAccess)
        {
            var departmentsToTest = new List<string> { "AAA BBB", "AAA BBB CCC", "AAA BBB CCC XXX", "AAA BBB CCC XXX EEE", "AAA BBB CCC XXX EEE FFF", "AAA BBB CCC YYY" };
            foreach (var dep in departmentsToTest)
                fixture.EnsureDepartment(dep);

            var testDepartment = "AAA BBB CCC XXX";
            var delegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var resourceOwner = fixture.AddResourceOwner(testDepartment);
            using var adminScope = fixture.UserScope(resourceOwner);

            var result = await Client.TestClientPostAsync<dynamic>($"/departments/{fullDepartment}/delegated-resource-owners", new
            {
                DateFrom = DateTime.Today.ToString("yyyy-MM-dd"),
                DateTo = DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd"),
                ResponsibleAzureUniqueId = delegatedResourceOwner.AzureUniqueId
            });

            if (expectingAccess)
                result.Should().BeSuccessfull();
            else
                result.Should().BeUnauthorized();
        }

        [Fact]
        public async Task PostDepartmentResponsible_Should_PersistFullDepartment_WhenSapIdProvided()
        {
            var testOrgUnit = fixture.AddOrgUnit("DEP RESP 1");

            using var adminScope = fixture.AdminScope();

            var delegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            var result = await Client.TestClientPostAsync($"/departments/{testOrgUnit.SapId}/delegated-resource-owners", new
            {
                DateFrom = DateTime.Today.ToString("yyyy-MM-dd"),
                DateTo = DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd"),
                ResponsibleAzureUniqueId = delegatedResourceOwner.AzureUniqueId
            });

            result.Should().BeSuccessfull();

            using (var dbScope = fixture.DbScope())
            {
                var item = dbScope.DbContext.DelegatedDepartmentResponsibles.FirstOrDefault(i => i.ResponsibleAzureObjectId == delegatedResourceOwner.AzureUniqueId && i.DepartmentId == testOrgUnit.FullDepartment);
                item.Should().NotBeNull();
            }
        }


        [Fact]
        public async Task OptionsDepartmentResponsible_ChildDepartmentsShouldBeDisAllowed_WhenDelegatedResourceOwner()
        {
            var testSector = "MY TPD LIN";
            var testDepartment = "MY TPD LIN DEP4";
            var delegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            fixture.EnsureDepartment(testSector, testSector, delegatedResourceOwner);
            fixture.EnsureDepartment(testDepartment);

            using var adminScope = fixture.UserScope(delegatedResourceOwner);
            var result = await Client.TestClientOptionsAsync($"/departments/{testDepartment}/delegated-resource-owners");
            result.Should().BeSuccessfull();
            result.CheckAllowHeader("OPTIONS, !DELETE, !POST, !GET");
        }
        [Fact]
        public async Task OptionsDepartmentResponsible_SiblingDepartmentsShouldBeDisallowed_WhenResourceOwner()
        {
            var testSector = "MY TPD LIN";
            var testDepartment = "MY TPD LIN DEP5";
            var delegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            fixture.EnsureDepartment(testSector, testSector);
            fixture.EnsureDepartment(testDepartment, testSector, delegatedResourceOwner);

            using var adminScope = fixture.UserScope(delegatedResourceOwner);
            var result = await Client.TestClientOptionsAsync($"/departments/{testDepartment}/delegated-resource-owners");
            result.Should().BeSuccessfull();
            result.CheckAllowHeader("OPTIONS, !DELETE, !POST, !GET");
        }

        [Fact]
        public async Task Options_As_NormalEmployee_Should_Be_Able_To_Get_DelegatedResourceOwners()
        {
            var testSector = "MY TPD LIN";
            var testDepartment = "MY TPD LIN DEP5";
            fixture.EnsureDepartment(testDepartment, testSector);

            var employee = fixture.AddProfile(FusionAccountType.Employee);

            using var adminScope = fixture.UserScope(employee);
            var result = await Client.TestClientOptionsAsync($"/departments/{testDepartment}/delegated-resource-owners");
            result.Should().BeSuccessfull();
            result.CheckAllowHeader("OPTIONS, !DELETE, !POST, GET");
        }

        [Fact]
        public async Task Options_As_Consultant_Should_Be_Able_To_Get_DelegatedResourceOwners()
        {
            var testSector = "MY TPD LIN";
            var testDepartment = "MY TPD LIN DEP5";
            fixture.EnsureDepartment(testDepartment, testSector);

            var employee = fixture.AddProfile(FusionAccountType.Consultant);

            using var adminScope = fixture.UserScope(employee);
            var result = await Client.TestClientOptionsAsync($"/departments/{testDepartment}/delegated-resource-owners");
            result.Should().BeSuccessfull();
            result.CheckAllowHeader("OPTIONS, !DELETE, !POST, GET");
        }

        [Fact]
        public async Task AddDepartmentResponsible_ShouldBeAllowed_WhenAdmin()
        {
            var testDepartment = "TPD LIN ORG TST";
            fixture.EnsureDepartment(testDepartment);
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientPostAsync<dynamic>($"/departments/{testDepartment}/delegated-resource-owners", new
            {
                DateFrom = DateTime.Today.ToString("yyyy-MM-dd"),
                DateTo = DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd"),
                ResponsibleAzureUniqueId = fakeResourceOwner.AzureUniqueId
            });

            var content = await resp.Response.Content.ReadAsStringAsync();
            resp.Response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task AddDepartmentResponsible_ShouldBeConflict_WhenAlreadyExists()
        {
            var testDepartment = "TPD LIN ORG TST2";
            fixture.EnsureDepartment(testDepartment);
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            using var adminScope = fixture.AdminScope();

            await Client.TestClientPostAsync<dynamic>($"/departments/{testDepartment}/delegated-resource-owners", new
            {
                DateFrom = DateTime.Today.ToString("yyyy-MM-dd"),
                DateTo = DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd"),
                ResponsibleAzureUniqueId = fakeResourceOwner.AzureUniqueId
            });
            var resp = await Client.TestClientPostAsync<dynamic>($"/departments/{testDepartment}/delegated-resource-owners", new
            {
                DateFrom = DateTime.Today.ToString("yyyy-MM-dd"),
                DateTo = DateTime.Today.AddMonths(1).ToString("yyyy-MM-dd"),
                ResponsibleAzureUniqueId = fakeResourceOwner.AzureUniqueId
            });

            resp.Response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }


        [Fact]
        public async Task DeleteDepartmentResponsible_ShouldBeAllowed_WhenAdmin()
        {
            var testDepartment = "TPD LIN ORG TST";
            fixture.EnsureDepartment(testDepartment);
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientPostAsync<dynamic>($"/departments/{testDepartment}/delegated-resource-owners", new
            {
                DateFrom = "2021-02-02",
                DateTo = "2022-02-05",
                ResponsibleAzureUniqueId = fakeResourceOwner.AzureUniqueId
            });

            resp = await Client.TestClientDeleteAsync<dynamic>(
                $"/departments/{testDepartment}/delegated-resource-owner/{fakeResourceOwner.AzureUniqueId}"
            );
            resp.Response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task DeleteDepartmentResponsible_ShouldBeIdempotent()
        {
            var testDepartment = "TPD LIN ORG TST";
            fixture.EnsureDepartment(testDepartment);
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientPostAsync<dynamic>($"/departments/{testDepartment}/delegated-resource-owners", new
            {
                DateFrom = "2021-02-02",
                DateTo = "2022-02-05",
                ResponsibleAzureUniqueId = fakeResourceOwner.AzureUniqueId
            });

            resp = await Client.TestClientDeleteAsync<dynamic>(
                $"/departments/{testDepartment}/delegated-resource-owner/{fakeResourceOwner.AzureUniqueId}"
            );

            resp = await Client.TestClientDeleteAsync<dynamic>(
                $"/departments/{testDepartment}/delegated-resource-owner/{fakeResourceOwner.AzureUniqueId}"
            );
            resp.Response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task RelevantDepartments_ShouldGetDataFromLineOrg()
        {
            var department = "PDP TST ABC";
            var parent = "PDP TST";
            var siblings = new[] { "PDP TST DEF", "PDP TST GHI" };
            var children = new[] { "PDP TST ABC QWE", "PDP TST ABC ASD" };

            foreach (var sibling in siblings)
            {
                fixture.EnsureDepartment(sibling);
            }
            foreach (var child in children)
            {
                fixture.EnsureDepartment(child);
            }
            LineOrgServiceMock.AddDepartment(parent, siblings.Union(new[] { department }).ToArray());
            LineOrgServiceMock.AddDepartment(department, children);

            using var adminScope = fixture.AdminScope();
            var resp = await Client.TestClientGetAsync<TestApiRelevantDepartments>($"/departments/{department}/related");
            resp.Should().BeSuccessfull();

            resp.Value.Parent.Name.Should().Be(parent);
            resp.Value.Siblings.Select(x => x.Name).Should().BeEquivalentTo(siblings);
            resp.Value.Children.Select(x => x.Name).Should().BeEquivalentTo(children);
        }

        [Fact]
        public async Task RelevantDepartments_ShouldGiveNotFound_WhenNoData()
        {
            var department = "PDP TST NOT FND";

            using var adminScope = fixture.AdminScope();
            var resp = await Client.TestClientGetAsync<TestApiRelevantDepartments>($"/departments/{department}/related");
            resp.Should().BeNotFound();
        }

        [Fact]
        public async Task PositionRelevantDepartments_ShouldGetDataFromLineOrg()
        {
            var department = "PDP TST ABC";
            var siblings = new[] { "PDP TST DEF", "PDP TST GHI" };
            var children = new[] { "PDP TST ABC QWE", "PDP TST ABC ASD" };

            LineOrgServiceMock.AddDepartment(department, children);
            LineOrgServiceMock.AddDepartment("PDP TST", siblings.Union(new[] { department }).ToArray());

            //foreach (var sibling in siblings)
            //    fixture.EnsureDepartment(sibling);
            //foreach (var child in children)
            //    fixture.EnsureDepartment(child);

            var project = new FusionTestProjectBuilder();
            var pos = project.AddPosition().WithEnsuredFutureInstances();
            pos.BasePosition = project
                .AddBasePosition("Senior Child Process Terminator", x => x.Department = department);

            using var adminScope = fixture.AdminScope();
            var resp = await Client.TestClientGetAsync(
                $"/projects/{pos.ProjectId}/positions/{pos.Id}/instances/{pos.Instances.First().Id}/relevant-departments",
                new { department = new TestDepartment(), relevant = new List<TestDepartment>() }
            );
            resp.Should().BeSuccessfull();

            resp.Value.department.Name.Should().Be(department);

            resp.Value.relevant.Select(x => x.Name).Should().Contain(siblings);
            resp.Value.relevant.Select(x => x.Name).Should().Contain(children);
            resp.Value.relevant.Select(x => x.Name).Should().Contain(department);
        }

        [Fact]
        public async Task PositionRelevantDepartments_ShouldNotFail_WhenBasePositionDepartmentIsEmpty()
        {
            var department = "PDP TST ABC";
            var siblings = new[] { "PDP TST DEF", "PDP TST GHI" };
            var children = new[] { "PDP TST ABC QWE", "PDP TST ABC ASD" };

            LineOrgServiceMock.AddDepartment(department, children);
            LineOrgServiceMock.AddDepartment("PDP TST", siblings);

            foreach (var sibling in siblings)
                fixture.EnsureDepartment(sibling);
            foreach (var child in children)
                fixture.EnsureDepartment(child);

            var project = new FusionTestProjectBuilder();
            var pos = project.AddPosition().WithEnsuredFutureInstances();
            pos.BasePosition = project
                .AddBasePosition("Senior Child Process Terminator", x => x.Department = null);

            using var adminScope = fixture.AdminScope();
            var resp = await Client.TestClientGetAsync(
                $"/projects/{pos.ProjectId}/positions/{pos.Id}/instances/{pos.Instances.First().Id}/relevant-departments",
                new { department = new TestDepartment(), relevant = new List<TestDepartment>() }
            );
            resp.Should().BeSuccessfull();
        }

        [Fact]
        public async Task PositionRelevantDepartments_ShouldNotFail_WhenBasePositionDepartmentDoesNotExist()
        {
            var department = "PDP TST ABC";
            var siblings = new[] { "PDP TST DEF", "PDP TST GHI" };
            var children = new[] { "PDP TST ABC QWE", "PDP TST ABC ASD" };

            LineOrgServiceMock.AddDepartment(department, children);
            LineOrgServiceMock.AddDepartment("PDP TST", siblings.Union(new[] { department }).ToArray());

            var project = new FusionTestProjectBuilder();
            var pos = project.AddPosition().WithEnsuredFutureInstances();
            pos.BasePosition = project
                .AddBasePosition("Senior Child Process Terminator", x => x.Department = "DPT NOT EXST");

            using var adminScope = fixture.AdminScope();
            var resp = await Client.TestClientGetAsync(
                $"/projects/{pos.ProjectId}/positions/{pos.Id}/instances/{pos.Instances.First().Id}/relevant-departments",
                new { department = new TestDepartment(), relevant = new List<TestDepartment>() }
            );
            resp.Should().BeSuccessfull();
        }

        [Fact]
        public async Task PositionRelevantDepartments_ShouldUseResponsiblityMatrix()
        {
            var department = "PDP TST ABC";
            var routedDepartment = "PDP TST ABC QWE";
            var siblings = new[] { "PDP TST DEF", "PDP TST GHI" };
            var children = new[] { routedDepartment, "PDP TST ABC ASD" };

            LineOrgServiceMock.AddDepartment(department, children);
            LineOrgServiceMock.AddDepartment("PDP TST", siblings.Union(new[] { department }).ToArray());

            var project = new FusionTestProjectBuilder();
            var pos = project.AddPosition().WithEnsuredFutureInstances();
            pos.BasePosition = project
                .AddBasePosition("Senior Child Process Terminator", x => x.Department = department);

            project.AddToMockService();

            using var adminScope = fixture.AdminScope();
            await Client.AddResponsibilityMatrixAsync(project, x =>
            {
                x.BasePositionId = pos.BasePosition.Id;
                x.Discipline = null;
                x.LocationId = null;
                x.Unit = "PDP TST ABC QWE";
            });

            var resp = await Client.TestClientGetAsync(
                $"/projects/{pos.ProjectId}/positions/{pos.Id}/instances/{pos.Instances.First().Id}/relevant-departments",
                new { department = new TestDepartment(), relevant = new List<TestDepartment>() }
            );
            resp.Should().BeSuccessfull();

            resp.Value.department.Name.Should().Be(routedDepartment);

            resp.Value.relevant.Select(x => x.Name).Should().Contain(siblings);
            resp.Value.relevant.Select(x => x.Name).Should().Contain(children);
            resp.Value.relevant.Select(x => x.Name).Should().Contain(routedDepartment);
        }

        [Fact]
        public async Task RelevantDepartment_ShouldBeNull_WhenBasePositionDepartmentIsEmptyString()
        {
            var ceo = fixture.AddProfile(FusionAccountType.Employee);
            LineOrgServiceMock.AddTestUser()
                .MergeWithProfile(ceo)
                .WithDepartment("")
                .WithFullDepartment("")
                .SaveProfile();

            var project = new FusionTestProjectBuilder();
            var pos = project.AddPosition().WithEnsuredFutureInstances();
            pos.BasePosition = project
                .AddBasePosition("Senior Child Process Terminator", x => x.Department = "");

            using var adminScope = fixture.AdminScope();
            var resp = await Client.TestClientGetAsync(
                $"/projects/{pos.ProjectId}/positions/{pos.Id}/instances/{pos.Instances.First().Id}/relevant-departments",
                new { department = new TestDepartment(), relevant = new List<TestDepartment>() }
            );
            resp.Should().BeSuccessfull();
            resp.Value.department.Should().BeNull();
        }

        private class TestApiRelevantDepartments
        {
            public TestDepartment Parent { get; set; }
            public List<TestDepartment> Children { get; set; }
            public List<TestDepartment> Siblings { get; set; }
        }
    }
}
