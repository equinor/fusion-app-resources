﻿using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Resources.Api.Controllers;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Mocks.OrgService;
using System;
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
            LineOrgServiceMock.AddTestUser().MergeWithProfile(fakeResourceOwner).AsResourceOwner().WithFullDepartment("TPD PRD LVL3 XXX").SaveProfile();


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

            var resp = await Client.TestClientGetAsync<TestDepartment>("/departments/NOT EXI ST ING?api-version=1.0-preview");

            resp.Response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetDepartment_Should_GetFromLineOrg_WhenNotInDb()
        {
            var department = "NOT IN DB";
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            LineOrgServiceMock.AddTestUser().MergeWithProfile(fakeResourceOwner).AsResourceOwner().WithFullDepartment(department).SaveProfile();
           
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync<TestDepartment>($"/departments/{department}?api-version=1.0-preview");

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

            var resp = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments?$search={fakeResourceOwner.Name}&api-version=1.0-preview");

            resp.Response.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Value.Should().Contain(x => x.Name == department);
        }


        [Fact]
        public async Task SearchShouldBeCaseInsensitive()
        {
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);
            LineOrgServiceMock.AddTestUser().MergeWithProfile(fakeResourceOwner).AsResourceOwner().SaveProfile();
           
            using var adminScope = fixture.AdminScope();

            var resp = await Client.TestClientGetAsync<List<TestDepartment>>($"/departments?$search={fakeResourceOwner.Name.ToUpper()}&api-version=1.0-preview");

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

            foreach (var sibling in siblings)
            {
                fixture.EnsureDepartment(sibling);
                LineOrgServiceMock.AddDepartment(sibling);
                LineOrgServiceMock.AddTestUser()
                    .MergeWithProfile(fixture.AddProfile(FusionAccountType.Employee))
                    .WithFullDepartment(sibling)
                    .AsResourceOwner()
                    .SaveProfile();
            }
            foreach (var child in children)
            {
                fixture.EnsureDepartment(child);
                LineOrgServiceMock.AddDepartment(child);
                LineOrgServiceMock.AddTestUser()
                    .MergeWithProfile(fixture.AddProfile(FusionAccountType.Employee))
                    .WithFullDepartment(child)
                    .AsResourceOwner()
                    .SaveProfile();
            }

                LineOrgServiceMock.AddDepartment("PDP TST", siblings);
            LineOrgServiceMock.AddDepartment("PDP TST ABC", children);

            using var adminScope = fixture.AdminScope();
            var resp = await Client.TestClientGetAsync<TestApiRelevantDepartments>($"/departments/{department}/related?api-version=1.0-preview");
            resp.Should().BeSuccessfull();

            resp.Value.Siblings.Select(x => x.Name).Should().BeEquivalentTo(siblings);
            resp.Value.Children.Select(x => x.Name).Should().BeEquivalentTo(children);
        }

        [Fact]
        public async Task RelevantDepartments_ShouldGiveNotFound_WhenNoData()
        {
            var department = "PDP TST NOT FND";
            
            using var adminScope = fixture.AdminScope();
            var resp = await Client.TestClientGetAsync<TestApiRelevantDepartments>($"/departments/{department}/related?api-version=1.0-preview");
            resp.Should().BeNotFound();
        }


        [Fact]
        public async Task PositionRelevantDepartments_ShouldGetDataFromLineOrg()
        {
            var department = "PDP TST ABC";
            var siblings = new[] { "PDP TST DEF", "PDP TST GHI" };
            var children = new[] { "PDP TST ABC QWE", "PDP TST ABC ASD" };

            fixture.EnsureDepartment(department);

            foreach (var sibling in siblings) {
                fixture.EnsureDepartment(sibling);
                LineOrgServiceMock.AddDepartment(sibling);
                LineOrgServiceMock.AddTestUser()
                    .MergeWithProfile(fixture.AddProfile(FusionAccountType.Employee))
                    .WithFullDepartment(sibling)
                    .AsResourceOwner()
                    .SaveProfile();
            }
            foreach (var child in children) {
                fixture.EnsureDepartment(child);
                LineOrgServiceMock.AddDepartment(child);
                LineOrgServiceMock.AddTestUser()
                    .MergeWithProfile(fixture.AddProfile(FusionAccountType.Employee))
                    .WithFullDepartment(child)
                    .AsResourceOwner()
                    .SaveProfile();
            }

            var project = new FusionTestProjectBuilder();
            var pos = project.AddPosition().WithEnsuredFutureInstances();
            pos.BasePosition = project
                .AddBasePosition("Senior Child Process Terminator", x => x.Department = department);

            LineOrgServiceMock.AddDepartment("PDP TST", siblings);
            LineOrgServiceMock.AddDepartment("PDP TST ABC", children);

            using var adminScope = fixture.AdminScope();
            var resp = await Client.TestClientGetAsync(
                $"/projects/{pos.ProjectId}/positions/{pos.Id}/instances/{pos.Instances.First().Id}/relevant-departments?api-version=1.0-preview",
                new { department = new TestDepartment(), relevant = new List<TestDepartment>() }
            );
            resp.Should().BeSuccessfull();

            resp.Value.relevant.Select(x => x.Name).Should().Contain(siblings);
            resp.Value.relevant.Select(x => x.Name).Should().Contain(children);
            resp.Value.relevant.Select(x => x.Name).Should().Contain(department);
        }

        private class TestApiRelevantDepartments
        {
            public List<TestDepartment> Children { get; set; }
            public List<TestDepartment> Siblings { get; set; }
        }
    }
}
