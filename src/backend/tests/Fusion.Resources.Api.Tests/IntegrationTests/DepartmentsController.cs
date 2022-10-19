using FluentAssertions;
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
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class DepartmentsController : IClassFixture<ResourceApiFixture>
    {
        private ResourceApiFixture fixture;
        private TestLoggingScope loggingScope;
        private object testUser;

        public DepartmentsController(ResourceApiFixture fixture, ITestOutputHelper output)
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
            //var department = "TPD LIN ORG TST1";
            var delegatedDepartment = "TPD LIN ORG TST1";
            var nonDelegatedDepartment = "Non delegated";

            var mainResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var delegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var nonDelegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            await RolesClientMock.AddPersonRole((System.Guid)delegatedResourceOwner.AzureUniqueId, new Fusion.Integration.Roles.RoleAssignment
            {
                Identifier = $"{Guid.NewGuid()}",
                RoleName = AccessRoles.ResourceOwner,
                Scope = new Fusion.Integration.Roles.RoleAssignment.RoleScope("OrgUnit", delegatedDepartment),
                ValidTo = DateTime.UtcNow.AddDays(1),
                Source = "Delegated project"
            });

            await RolesClientMock.AddPersonRole((System.Guid)delegatedResourceOwner.AzureUniqueId, new Fusion.Integration.Roles.RoleAssignment
            {
                Identifier = $"{Guid.NewGuid()}",
                RoleName = AccessRoles.ResourceOwner,
                Scope = new Fusion.Integration.Roles.RoleAssignment.RoleScope("OrgUnit", delegatedDepartment),
                ValidTo = DateTime.UtcNow.AddDays(1),
                Source = "Delegated project"
            });

            await RolesClientMock.AddPersonRole((System.Guid)nonDelegatedResourceOwner.AzureUniqueId, new Fusion.Integration.Roles.RoleAssignment
            {
                Identifier = $"{Guid.NewGuid()}",
                RoleName = AccessRoles.ResourceOwner,
                Scope = new Fusion.Integration.Roles.RoleAssignment.RoleScope("OrgUnit", nonDelegatedDepartment),
                ValidTo = DateTime.UtcNow.AddDays(1),
                Source = "Test project"
            });

            LineOrgServiceMock.AddTestUser().MergeWithProfile(mainResourceOwner).AsResourceOwner().WithFullDepartment(delegatedDepartment).SaveProfile();
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments?$search={mainResourceOwner.Name}");

            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Value.Should().Contain(x => x.Name == delegatedDepartment && x.DelegatedResponsibles.Count >= 1);
        }

        [Fact]
        public async Task GetDepartment_Should_GetDelegatedResponsibles_FromGetDepartmentString()
        {
            //var department = "TPD LIN ORG TST1";
            var delegatedDepartment = "TPD LIN ORG TST1";
            var nonDelegatedDepartment = "Non delegated";

            var mainResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var delegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var nonDelegatedResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            await RolesClientMock.AddPersonRole((System.Guid)delegatedResourceOwner.AzureUniqueId, new Fusion.Integration.Roles.RoleAssignment
            {
                Identifier = $"{Guid.NewGuid()}",
                RoleName = AccessRoles.ResourceOwner,
                Scope = new Fusion.Integration.Roles.RoleAssignment.RoleScope("OrgUnit", delegatedDepartment),
                ValidTo = DateTime.UtcNow.AddDays(1),
                Source = "Test project"
            });

            await RolesClientMock.AddPersonRole((System.Guid)nonDelegatedResourceOwner.AzureUniqueId, new Fusion.Integration.Roles.RoleAssignment
            {
                Identifier = $"{Guid.NewGuid()}",
                RoleName = AccessRoles.ResourceOwner,
                Scope = new Fusion.Integration.Roles.RoleAssignment.RoleScope("OrgUnit", nonDelegatedDepartment),
                ValidTo = DateTime.UtcNow.AddDays(1),
                Source = "Test project"
            });

            LineOrgServiceMock.AddTestUser().MergeWithProfile(mainResourceOwner).AsResourceOwner().WithFullDepartment(delegatedDepartment).SaveProfile();
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments/{delegatedDepartment}");

            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Value.Should().Contain(x => x.Name == delegatedDepartment && x.DelegatedResponsibles.First().AzureUniquePersonId.Equals(delegatedResourceOwner.AzureUniqueId));
        }

        [Fact]
        public async Task SearchShouldBeCaseInsensitive()
        {
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            LineOrgServiceMock.AddTestUser().MergeWithProfile(fakeResourceOwner).AsResourceOwner().SaveProfile();

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments?$search={fakeResourceOwner.Name.ToUpper()}");

            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Value.Should().Contain(x => x.Name == fakeResourceOwner.FullDepartment);
        }

        [Fact]
        public async Task AddDepartmentResponsible_ShouldBeAllowed_WhenAdmin()
        {
            var testDepartment = "TPD LIN ORG TST";
            fixture.EnsureDepartment(testDepartment);
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientPostAsync<dynamic>($"/departments/{testDepartment}/delegated-resource-owner", new
            {
                DateFrom = "2021-02-02",
                DateTo = "2022-02-05",
                ResponsibleAzureUniqueId = fakeResourceOwner.AzureUniqueId
            });

            resp.Response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task DeleteDepartmentResponsible_ShouldBeAllowed_WhenAdmin()
        {
            var testDepartment = "TPD LIN ORG TST";
            fixture.EnsureDepartment(testDepartment);
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientPostAsync<dynamic>($"/departments/{testDepartment}/delegated-resource-owner", new
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

            var resp = await Client.TestClientPostAsync<dynamic>($"/departments/{testDepartment}/delegated-resource-owner", new
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
            LineOrgServiceMock.AddDepartment("PDP TST", siblings);
            LineOrgServiceMock.AddDepartment("PDP TST ABC", children);

            using var adminScope = fixture.AdminScope();
            var resp = await Client.TestClientGetAsync<TestApiRelevantDepartments>($"/departments/{department}/related");
            resp.Should().BeSuccessfull();

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
            LineOrgServiceMock.AddDepartment("PDP TST", siblings);

            foreach (var sibling in siblings) fixture.EnsureDepartment(sibling);
            foreach (var child in children) fixture.EnsureDepartment(child);

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

            foreach (var sibling in siblings) fixture.EnsureDepartment(sibling);
            foreach (var child in children) fixture.EnsureDepartment(child);

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
            LineOrgServiceMock.AddDepartment("PDP TST", siblings);

            foreach (var sibling in siblings) fixture.EnsureDepartment(sibling);
            foreach (var child in children) fixture.EnsureDepartment(child);

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
            LineOrgServiceMock.AddDepartment("PDP TST", siblings);

            foreach (var sibling in siblings) fixture.EnsureDepartment(sibling);
            foreach (var child in children) fixture.EnsureDepartment(child);

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
            public List<TestDepartment> Children { get; set; }
            public List<TestDepartment> Siblings { get; set; }
        }
    }
}