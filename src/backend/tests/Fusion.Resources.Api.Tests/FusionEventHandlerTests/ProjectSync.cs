using FluentAssertions;
using Fusion.Integration.Org;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Resources.Database;
using Fusion.Testing.Mocks.OrgService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fusion.Resources.Api.Tests.FusionEventHandlerTests
{
    public class ProjectSync : IClassFixture<ResourceApiFixture>
    {
        private ResourcesDbContext db;
        private IProjectOrgResolver orgServiceMock;
        private FusionTestProjectBuilder testProject;
        OrgProjectHandler handler;

        public ProjectSync(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            using var scope = fixture.ApiFactory.Services.CreateScope();
            db = new ResourcesDbContext(
              new DbContextOptionsBuilder<ResourcesDbContext>()
              .UseInMemoryDatabase($"unit-test-db-{Guid.NewGuid()}")
              .Options
            );
            orgServiceMock = scope.ServiceProvider.GetRequiredService<IProjectOrgResolver>();
            testProject = new FusionTestProjectBuilder()
                .WithPositions(1)
                .AddToMockService();

            handler = new OrgProjectHandler(
                db,
                orgServiceMock,
                new Mock<ILogger<OrgProjectHandler>>(MockBehavior.Loose).Object
            );

            db.Projects.Add(new Database.Entities.DbProject
            {
                Id = Guid.NewGuid(),
                DomainId = "TBD",
                Name = "TBD",
                OrgProjectId = testProject.Project.ProjectId
            });
            db.SaveChanges();
        }

        [Fact]
        public async Task ShouldUpdateProjectOnProjectUpdateEvent()
        {
            var payload = JsonSerializer.Serialize(new
            {
                ItemId = testProject.Project.ProjectId,
                Type = "ProjectUpdated",
            });

            var context = (Events.MessageContext)FormatterServices.GetUninitializedObject(typeof(Events.MessageContext));
            context.Message = new Microsoft.Azure.ServiceBus.Message
            {
                Body = Encoding.UTF8.GetBytes(payload)
            };
            context.Event = new Events.CloudEventV1
            {
                Data = payload
            };

            await handler.ProcessMessageAsync(context, payload, CancellationToken.None);

            var updated = await db.Projects
                .FirstOrDefaultAsync(p => p.OrgProjectId == testProject.Project.ProjectId);

            updated.Name.Should().Be(testProject.Project.Name);
            updated.DomainId.Should().Be(testProject.Project.DomainId);
        }
    }
}
