﻿using FluentAssertions;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using System;
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

            var resp = await Client.TestClientGetAsync($"/departments/{department}/resources/requests/unassigned?api-version=1.0-preview", new { value = new[] { new { id = Guid.Empty } } });
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

            var resp = await Client.TestClientGetAsync($"/departments/{department}/resources/requests/unassigned?api-version=1.0-preview", new { value = new[] { new { id = Guid.Empty } } });
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

            var resp = await Client.TestClientGetAsync($"/departments/{department}/resources/requests/unassigned?api-version=1.0-preview", new { value = new[] { new { id = Guid.Empty } } });
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

            var resp = await Client.TestClientGetAsync($"/departments/{department}/resources/requests/unassigned?api-version=1.0-preview", new { value = new[] { new { id = Guid.Empty } } });
            resp.Value.value.Should().NotContain(r => r.id == unassignedRequest.Id);
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