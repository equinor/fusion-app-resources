using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Resources.Api.Controllers;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
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
        public async Task CreateSector_ShouldBeSuccessfull()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientPostAsync<TestDepartment>("/departments?api-version=1.0-preview", new
            {
                DepartmentId = "TPD PRD TXT",
            });

            resp.Response.StatusCode.Should().Be(HttpStatusCode.Created);
            resp.Value.Name.Should().Be("TPD PRD TXT");
            resp.Value.Sector.Should().BeNull();
        }

        [Fact]
        public async Task AddDepartment_Should_BeSuccessfull()
        {
            using var adminScope = fixture.AdminScope();

            fixture.EnsureDepartment("TPD PRD LVL3");

            var resp = await Client.TestClientPostAsync<TestDepartment>("/departments?api-version=1.0-preview", new
            {
                DepartmentId = "TPD PRD LVL3 LVL4",
                SectorId = "TPD PRD LVL3",
            });

            resp.Response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task AddeDepartment_Should_BeSuccessfull_WhenExistsInLineOrg()
        {
            using var adminScope = fixture.AdminScope();

            fixture.EnsureDepartment("TPD PRD LVL3");
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var lineorgData = new
            {
                Count = 1,
                TotalCount = 1,
                Value = new[]
                {
                    new
                    {
                        fakeResourceOwner.AzureUniqueId,
                        fakeResourceOwner.Name,
                        fakeResourceOwner.Mail,
                        IsResourceOwner = true,
                        FullDepartment = "TPD PRD LVL3 XXX"
                    }
                }
            };

            fixture.LineOrg.WithResponse("/lineorg/persons", lineorgData);

            var resp = await Client.TestClientPostAsync<TestDepartment>("/departments?api-version=1.0-preview", new
            {
                DepartmentId = "TPD PRD LVL3 XXX",
                SectorId = "TPD PRD LVL3",
            });

            resp.Response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task AddDepartment_ShouldGiveBadRequest_WhenSectorDoesNotExist()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientPostAsync<TestDepartment>("/departments?api-version=1.0-preview", new
            {
                DepartmentId = "TPD PRD TST DPT",
                SectorId = "TPD PRD TST"
            });

            resp.Response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateDepartment_ShouldGiveNotFound_WhenDepartmentNotInDb()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientPutAsync<TestDepartment>("/departments/TPD PRD TST DPT?api-version=1.0-preview", new
            {
                SectorId = "TPD PRD TST"
            });

            resp.Response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetDepartment_ShouldGiveNotFound_WhenNotInLineOrg()
        {
            using var adminScope = fixture.AdminScope();

            var lineorgData = new
            {
                Count = 0,
                TotalCount = 0,
                Value = Array.Empty<object>()
            };

            fixture.LineOrg.WithResponse("/lineorg/persons", lineorgData);

            var resp = await Client.TestClientGetAsync<TestDepartment>("/departments/TPD LIN ORG TST?api-version=1.0-preview");

            resp.Response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetDepartment_Should_GetFromLineOrg_WhenNotInDb()
        {
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var lineorgData = new
            {
                Count = 1,
                TotalCount = 1,
                Value = new[]
                {
                    new
                    {
                        fakeResourceOwner.AzureUniqueId,
                        fakeResourceOwner.Name,
                        fakeResourceOwner.Mail,
                        IsResourceOwner = true,
                        FullDepartment = "TPD LIN ORG TST"
                    }
                }
            };

            fixture.LineOrg.WithResponse("/lineorg/persons", lineorgData);

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync<TestDepartment>("/departments/TPD LIN ORG TST?api-version=1.0-preview");

            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Value.Name.Should().Be("TPD LIN ORG TST");
        }

        [Fact]
        public async Task SearchDepartment_Should_GetFromLineOrg_WhenNotInDb()
        {
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            var lineorgData = new
            {
                Count = 1,
                TotalCount = 1,
                Value = new[]
                {
                    new
                    {
                        fakeResourceOwner.AzureUniqueId,
                        fakeResourceOwner.Name,
                        fakeResourceOwner.Mail,
                        IsResourceOwner = true,
                        FullDepartment = "TPD LIN ORG TST"
                    }
                }
            };

            fixture.LineOrg.WithResponse("/lineorg/persons", lineorgData);

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments?$search={fakeResourceOwner.Name}&api-version=1.0-preview");

            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Value.Should().Contain(x => x.Name == "TPD LIN ORG TST");
        }

        [Fact]
        public async Task AddDepartmentResponsible_ShouldBeAllowed_WhenAdmin()
        {
            var testDepartment = "TPD LIN ORG TST";
            fixture.EnsureDepartment(testDepartment);
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientPostAsync<dynamic>($"/departments/{testDepartment}/delegated-resource-owner?api-version=1.0-preview", new
            {
                DateFrom = "2021-02-02",
                DateTo = "2022-02-05",
                ResponsibleAzureUniqueId = fakeResourceOwner.AzureUniqueId
            });

            resp.Response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task RelevantDepartments_ShouldGetDataFromLineOrg()
        {
            var department = "PDP TST ABC";
            var siblings = new[] { "PDP TST DEF", "PDP TST GHI" };
            var children = new[] { "PDP TST ABC QWE", "PDP TST ABC ASD" };
            fixture.EnsureDepartment(department);

            foreach (var sibling in siblings) fixture.EnsureDepartment(sibling);
            foreach (var child in children) fixture.EnsureDepartment(child);

            fixture.LineOrg.WithResponse("/lineorg/departments/PDP TST", new { children = new[] { new { name = siblings[0], fullName = siblings[0] }, new { name = siblings[1], fullName = siblings[1] } } });
            fixture.LineOrg.WithResponse("/lineorg/departments/PDP TST ABC", new { children = new[] { new { name = children[0], fullName = children[0] }, new { name = children[1], fullName = children[1] } } });

            using var adminScope = fixture.AdminScope();
            var resp = await Client.TestClientGetAsync<TestApiRelevantDepartments>($"/departments/{department}/related?api-version=1.0-preview");
            resp.Should().BeSuccessfull();

            resp.Value.Siblings.Should().BeEquivalentTo(siblings);
            resp.Value.Children.Should().BeEquivalentTo(children);
        }

        [Fact]
        public async Task RelevantDepartments_ShouldGiveNotFound_WhenNoData()
        {
            var department = "PDP TST ABC";
            
            using var adminScope = fixture.AdminScope();
            var resp = await Client.TestClientGetAsync<TestApiRelevantDepartments>($"/departments/{department}/related?api-version=1.0-preview");
            resp.Should().BeNotFound();
        }

        private class TestApiRelevantDepartments
        {
            public List<string> Children { get; set; }
            public List<string> Siblings { get; set; }
        }
    }
}
