using FluentAssertions;
using Fusion.Integration.Profile;
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
        }

        private HttpClient Client => fixture.ApiFactory.CreateClient();


        [Fact]
        public async Task ShouldCreateSectorSuccessfully()
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
        public async Task ShouldCreateDepartmentSuccessfully()
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
        public async Task ShouldGiveBadRequestWhenSectorDoesNotExist()
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
        public async Task ShouldGiveNotFoundWhenUpdatingNonExistantDepartment()
        {
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientPutAsync<TestDepartment>("/departments/TPD PRD TST DPT?api-version=1.0-preview", new
            {
                SectorId = "TPD PRD TST"
            });

            resp.Response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ShouldGiveNotFoundWhenRetreivingNotInLineOrg()
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
        public async Task ShouldGetDepartmentNotInDbWhenInLineOrg()
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
        public async Task SearchShouldGetDepartmentNotInDbWhenInLineOrg()
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
        public async Task ShouldAllowAdminToAddDepartmentResponsible()
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
    }
}
