﻿using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Mocks.OrgService;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.IntegrationTests.UnassignedRequests
{
    public class DepartmentUnassignedTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        private ResourceApiFixture fixture;
        private FusionTestProjectBuilder testProject;
        private readonly TestLoggingScope loggingScope;

        public DepartmentUnassignedTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);
        }

        private HttpClient Client => fixture.ApiFactory.CreateClient();


        [Fact]
        public async Task ShouldReturnRequest_WhenBasePositionDepartmentStartsWith()
        {
            var department = "TPD PRD MY TEST DEP1";
            fixture.EnsureDepartment(department);

            var bp = testProject.AddBasePosition($"{Guid.NewGuid()}", s => s.Department = "TPD PRD MY TEST DEP");
            var testPosition = testProject.AddPosition().WithBasePosition(bp);


            using var adminScope = fixture.AdminScope();

            var unassignedRequest = await Client.CreateAndStartDefaultRequestOnPositionAsync(testProject, testPosition);

            var resp = await Client.TestClientGetAsync($"/departments/{department}/resources/requests/unassigned", new { value = new[] { new { id = Guid.Empty } } });
            resp.Value.value.Should().Contain(r => r.id == unassignedRequest.Id);
        }

        [Fact]
        public async Task ShouldReturnRequest_WhenBasePositionDepartmentTargetsSector()
        {
            var department = "TPD PRD MY TEST DEP1";
            fixture.EnsureDepartment(department);

            var bp = testProject.AddBasePosition($"{Guid.NewGuid()}", s => s.Department = "TPD PRD MY TEST");
            var testPosition = testProject.AddPosition().WithBasePosition(bp);


            using var adminScope = fixture.AdminScope();

            var unassignedRequest = await Client.CreateAndStartDefaultRequestOnPositionAsync(testProject, testPosition);

            var resp = await Client.TestClientGetAsync($"/departments/{department}/resources/requests/unassigned", new { value = new[] { new { id = Guid.Empty } } });
            resp.Value.value.Should().Contain(r => r.id == unassignedRequest.Id);
        }

        [Fact]
        public async Task ShouldReturnRequest_WhenDepartmentStartsWithBasePositionDepartment()
        {
            var department = "TPD PRD MY";
            fixture.EnsureDepartment(department);

            var bp = testProject.AddBasePosition($"{Guid.NewGuid()}", s => s.Department = "TPD PRD MY TEST");
            var testPosition = testProject.AddPosition().WithBasePosition(bp);


            using var adminScope = fixture.AdminScope();

            var unassignedRequest = await Client.CreateAndStartDefaultRequestOnPositionAsync(testProject, testPosition);

            var resp = await Client.TestClientGetAsync($"/departments/{department}/resources/requests/unassigned", new { value = new[] { new { id = Guid.Empty } } });
            resp.Value.value.Should().Contain(r => r.id == unassignedRequest.Id);
        }

        [Fact]
        public async Task ShouldNotReturnRequest_WhenBasePositionDepartmentNotInPath()
        {
            var department = "TPD PRD MY TEST DEP1";
            fixture.EnsureDepartment(department);

            var bp = testProject.AddBasePosition($"{Guid.NewGuid()}", s => s.Department = "TPD PRD OTHER TEST DEP");
            var testPosition = testProject.AddPosition().WithBasePosition(bp);


            using var adminScope = fixture.AdminScope();

            var unassignedRequest = await Client.CreateAndStartDefaultRequestOnPositionAsync(testProject, testPosition);

            var resp = await Client.TestClientGetAsync($"/departments/{department}/resources/requests/unassigned", new { value = new[] { new { id = Guid.Empty } } });
            resp.Value.value.Should().NotContain(r => r.id == unassignedRequest.Id);
        }

        [Fact]
        public async Task ShouldExcludeDraftRequests()
        {
            var department = "TPD PRD MY TEST DEP1";
            fixture.EnsureDepartment(department);

            var bp = testProject.AddBasePosition($"{Guid.NewGuid()}", s => s.Department = department);

            using var adminScope = fixture.AdminScope();

            var unassignedRequest = await Client.CreateDefaultRequestAsync(testProject, 
                _=> { }, 
                pos => pos.WithBasePosition(bp)
            );

            var resp = await Client.TestClientGetAsync($"/departments/{department}/resources/requests/unassigned", new { value = new[] { new { id = Guid.Empty } } });
            resp.Value.value.Should().NotContain(r => r.id == unassignedRequest.Id);
        }

        [Fact]
        public async Task ShouldExpandPositionInstances()
        {
            var basePositionDepartment = "TPD PRD MY TEST DPT";
            var department = "TPD PRD MY TEST DPT1";
            fixture.EnsureDepartment(department);

            var bp = testProject.AddBasePosition($"{Guid.NewGuid()}", s => s.Department = basePositionDepartment);
            var testPosition = testProject.AddPosition().WithBasePosition(bp).WithInstances(3);

            using var adminScope = fixture.AdminScope();

            var unassignedRequest = await Client.CreateAndStartDefaultRequestOnPositionAsync(testProject, testPosition);

            var resp = await Client.TestClientGetAsync($"/departments/{department}/resources/requests/unassigned", 
                new { value = new[] { new { id = Guid.Empty, orgPosition = new { instances = Array.Empty<object>() } } } });
            resp.Value.value.Single().orgPosition.instances.Should().HaveCount(3);
        }

        [Fact]
        public async Task SupportsOnlyCount()
        {
            var basePositionDepartment = "TPD PRD MY TEST DEP";
            var department = "TPD PRD MY TEST DEP1";
            fixture.EnsureDepartment(department);

            var bp = testProject.AddBasePosition($"{Guid.NewGuid()}", s => s.Department = basePositionDepartment);

            using var adminScope = fixture.AdminScope();

            await Client.CreateAndStartDefaultRequestOnPositionAsync(testProject, testProject.AddPosition().WithBasePosition(bp));
            await Client.CreateAndStartDefaultRequestOnPositionAsync(testProject, testProject.AddPosition().WithBasePosition(bp));

            var resp = await Client.TestClientGetAsync($"/departments/{department}/resources/requests/unassigned?$count=only",
                new { totalCount = -1 });
            resp.Value.totalCount.Should().Be(2);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public Task InitializeAsync()
        {
            // Mock project
            testProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .AddToMockService();

            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(testProject.Project);
         
            return Task.CompletedTask;
        }
    }
}
