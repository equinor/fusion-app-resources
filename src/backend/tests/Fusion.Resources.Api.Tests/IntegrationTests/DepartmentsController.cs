using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Mocks.OrgService;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Fusion.Testing.Mocks.LineOrgService;
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
        public async Task DeleteDepartmentResponsible_ShouldGive404_WhenNotExisting()
        {
            var testDepartment = "TPD LIN ORG TST";
            fixture.EnsureDepartment(testDepartment);
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientDeleteAsync<dynamic>(
                $"/departments/{testDepartment}/delegated-resource-owner/{fakeResourceOwner.AzureUniqueId}"
            );
            resp.Response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
            await Client.AddResponsibilityMatrixAsync(project, x => {
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

        #region Auto approval

        [Fact]
        public async Task UpdateAutoApproval_ShouldBeBadRequest_WhenInvalidMode()
        {
            // Department does not need to exist, the payload is invalid so should result in bad request before checking if dep exists.
            using var adminScope = fixture.AdminScope();
            var resp = await Client.TestClientPatchAsync("/departments/NOT HERE", new
            {
                autoApproval = new { enabled = true, mode = "Invalid" }
            }, new { });
            resp.Should().BeBadRequest();

            // { ... "errors":{"autoApproval.mode":["Invalid value, supported types: All, Direct"]}}
        }

        [Fact]
        public async Task AutoApproval_Update_ShouldSetEnableAutoApproval()
        {
            // Department does not need to exist, the payload is invalid so should result in bad request before checking if dep exists.
            using var adminScope = fixture.AdminScope();
            
            var testDepartmentString = "1 TEST AUTO APPROVAL";
            fixture.EnsureDepartment(testDepartmentString);

            var resp = await Client.TestClientPatchAsync($"/departments/{testDepartmentString}", new
            {
                autoApproval = new { enabled = true, mode = "All" }
            }, new {
                autoApproval = new { enabled = false, mode = string.Empty }
            });
            resp.Should().BeSuccessfull();
            resp.Value.autoApproval.Should().NotBeNull();
            resp.Value.autoApproval.enabled.Should().BeTrue();
            resp.Value.autoApproval.mode.Should().BeEquivalentTo("All");
        }

        [Fact]
        public async Task AutoApproval_Update_ShouldDeleteAutoApproval_WhenSettingNull()
        {
            // Department does not need to exist, the payload is invalid so should result in bad request before checking if dep exists.
            using var adminScope = fixture.AdminScope();

            var testDepartmentString = "2 TEST AUTO APPROVAL";
            fixture.EnsureDepartment(testDepartmentString);

            await Client.SetDepartmentAutoApproval(testDepartmentString, true);

            var resp = await Client.TestClientPatchAsync($"/departments/{testDepartmentString}", new
            {
                autoApproval = (object)null
            }, new
            {
                autoApproval = new { enabled = false, mode = string.Empty }
            });
            resp.Should().BeSuccessfull();
            resp.Value.autoApproval.Should().BeNull();
        }

        [Fact]
        public async Task AutoApproval_Update_ShouldNotDoAnything_WhenSettingNullAndExistingIsNull()
        {
            // Department does not need to exist, the payload is invalid so should result in bad request before checking if dep exists.
            using var adminScope = fixture.AdminScope();

            var testDepartmentString = "3 TEST AUTO APPROVAL";
            fixture.EnsureDepartment(testDepartmentString);

            var resp = await Client.TestClientPatchAsync($"/departments/{testDepartmentString}", new
            {
                autoApproval = (object)null
            }, new
            {
                autoApproval = new { enabled = false, mode = string.Empty }
            });
            resp.Should().BeSuccessfull();
            resp.Value.autoApproval.Should().BeNull();
        }

        [Fact]
        public async Task AutoApproval_ShouldInheritFromParent_WhenModeIsAll()
        {
            // Department does not need to exist, the payload is invalid so should result in bad request before checking if dep exists.
            using var adminScope = fixture.AdminScope();

            #region Arrange
            var parentDepartment = "1 AP INH";
            var testDepartmentString = "1 AP INH DEP";

            fixture.EnsureDepartment(parentDepartment);
            fixture.EnsureDepartment(testDepartmentString);

            await Client.SetDepartmentAutoApproval(parentDepartment, true, "All");
            #endregion


            var resp = await Client.TestClientGetAsync($"/departments/{testDepartmentString}", new
            {
                autoApproval = new 
                { 
                    enabled = true, 
                    mode = string.Empty, 
                    inherited = false, 
                    inheritedFrom = string.Empty 
                }
            });
            resp.Should().BeSuccessfull();
            resp.Value.autoApproval.Should().NotBeNull();
            resp.Value.autoApproval.enabled.Should().BeTrue();
            resp.Value.autoApproval.inherited.Should().BeTrue();
            resp.Value.autoApproval.inheritedFrom.Should().BeEquivalentTo(parentDepartment);
        }

        [Fact]
        public async Task AutoApproval_ShouldBreakInheritence_WhenTwoParentsIncludeChildren()
        {
            // Department does not need to exist, the payload is invalid so should result in bad request before checking if dep exists.
            using var adminScope = fixture.AdminScope();

            #region Arrange
            var parentDepartment = "2 AP INH";
            var parentInheritenceBreaker = "2 AP INH BRK";
            var testDepartmentString = "2 AP INH BRK DEP";

            fixture.EnsureDepartment(parentDepartment);
            fixture.EnsureDepartment(parentInheritenceBreaker);
            fixture.EnsureDepartment(testDepartmentString);

            await Client.SetDepartmentAutoApproval(parentDepartment, true, "All");
            await Client.SetDepartmentAutoApproval(parentInheritenceBreaker, false, "All");

            #endregion

            var resp = await Client.TestClientGetAsync($"/departments/{testDepartmentString}", new
            {
                autoApproval = new
                {
                    enabled = true,
                    mode = string.Empty,
                    inherited = false,
                    inheritedFrom = string.Empty
                }
            });
            resp.Should().BeSuccessfull();
            resp.Value.autoApproval.Should().NotBeNull();
            resp.Value.autoApproval.enabled.Should().BeFalse();
            resp.Value.autoApproval.inherited.Should().BeTrue();
            resp.Value.autoApproval.inheritedFrom.Should().BeEquivalentTo(parentInheritenceBreaker);
        }

        [Fact]
        public async Task AutoApproval_ShouldNotBreakInheritence_WhenParentDoesNotIncludeChildren()
        {
            // Department does not need to exist, the payload is invalid so should result in bad request before checking if dep exists.
            using var adminScope = fixture.AdminScope();

            #region Arrange
            var parentDepartment = "3 AP INH";
            var parentInheritenceBreaker = "3 AP INH BRK";
            var testDepartmentString = "3 AP INH BRK DEP";

            fixture.EnsureDepartment(parentDepartment);
            fixture.EnsureDepartment(parentInheritenceBreaker);
            fixture.EnsureDepartment(testDepartmentString);

            await Client.SetDepartmentAutoApproval(parentDepartment, true, "All");
            await Client.SetDepartmentAutoApproval(parentInheritenceBreaker, false, "Direct");

            #endregion

            var resp = await Client.TestClientGetAsync($"/departments/{testDepartmentString}", new
            {
                autoApproval = new
                {
                    enabled = true,
                    mode = string.Empty,
                    inherited = false,
                    inheritedFrom = string.Empty
                }
            });
            resp.Should().BeSuccessfull();
            resp.Value.autoApproval.Should().NotBeNull();
            resp.Value.autoApproval.enabled.Should().BeTrue();
            resp.Value.autoApproval.inherited.Should().BeTrue();
            resp.Value.autoApproval.inheritedFrom.Should().BeEquivalentTo(parentDepartment);
        }

        [Fact]
        public async Task AutoApproval_ShouldOverrideInheritence_WhenUpdatingOnDepartment()
        {
            // Department does not need to exist, the payload is invalid so should result in bad request before checking if dep exists.
            using var adminScope = fixture.AdminScope();

            #region Arrange
            var parentDepartment = "4 AP INH";
            var testDepartmentString = "4 AP INH BRK DEP";

            fixture.EnsureDepartment(parentDepartment);
            fixture.EnsureDepartment(testDepartmentString);

            await Client.SetDepartmentAutoApproval(parentDepartment, true, "All");
            await Client.SetDepartmentAutoApproval(testDepartmentString, false, "Direct");

            #endregion

            var resp = await Client.TestClientGetAsync($"/departments/{testDepartmentString}", new
            {
                autoApproval = new
                {
                    enabled = true,
                    mode = string.Empty,
                    inherited = false,
                    inheritedFrom = string.Empty
                }
            });
            resp.Should().BeSuccessfull();
            resp.Value.autoApproval.Should().NotBeNull();
            resp.Value.autoApproval.enabled.Should().BeFalse();
            resp.Value.autoApproval.inherited.Should().BeFalse();
            resp.Value.autoApproval.inheritedFrom.Should().BeNull();
        }

        #endregion

        private class TestApiRelevantDepartments
        {
            public List<TestDepartment> Children { get; set; }
            public List<TestDepartment> Siblings { get; set; }
        }
    }
}
