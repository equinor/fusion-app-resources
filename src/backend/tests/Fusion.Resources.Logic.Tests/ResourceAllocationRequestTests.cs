using FluentAssertions;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;
using Fusion.Integration.Profile;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using MediatR;
using Xunit;
using static Fusion.Resources.Logic.Commands.ResourceAllocationRequest;

namespace Fusion.Resources.Logic.Tests
{
    public class ResourceAllocationRequestTests : IAsyncLifetime
    {
        private readonly Guid testOrgId = Guid.NewGuid();
        private readonly ResourcesDbContext dbContext;
        private readonly Mock<IProjectOrgResolver> orgResolverMock;
        private readonly Mock<IProfileService> profileServiceMock;
        private readonly Mock<IMediator> mediatorMock;
        private readonly ApiProjectV2 testProject;
        private Guid testRequestId = Guid.NewGuid();

        public ResourceAllocationRequestTests()
        {
            var dbOptions = new DbContextOptionsBuilder<ResourcesDbContext>()
                .UseInMemoryDatabase(nameof(ResourceAllocationRequestTests))
                .Options;

            dbContext = new ResourcesDbContext(dbOptions);

            var orgProject = new OrgProjectId(testOrgId);
            orgResolverMock = new Mock<IProjectOrgResolver>();
            testProject = new ApiProjectV2 { ProjectId = orgProject.ProjectId!.Value, Name = "TestProject", DomainId = Guid.NewGuid().ToString() };
            orgResolverMock.Setup(r => r.ResolveProjectAsync(It.IsAny<OrgProjectId>())).ReturnsAsync(testProject);

            profileServiceMock = new Mock<IProfileService>();

            mediatorMock = new Mock<IMediator>();

        }

        public async Task InitializeAsync()
        {
            var person = new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), AccountType = $"{FusionAccountType.Employee}" };
            await dbContext.Persons.AddAsync(person);

            var handler = new Create.Handler(profileServiceMock.Object, orgResolverMock.Object, dbContext, mediatorMock.Object);
            var command = new Create(testProject.ProjectId)
                .WithType("Normal");
            command.SetEditor(person.Id, person);

            await handler.Handle(command, CancellationToken.None);
        }

        public async Task DisposeAsync()
        {
            return;
        }

        [Fact]
        public async Task DeleteRequest_NonExisting_ShouldBe_BadRequest()
        {

            var handler = new Delete.Handler(dbContext);
            var command = new Delete(Guid.NewGuid());

            var response = await handler.Handle(command, new CancellationToken());
            response.Should().Be(false);
        }

        [Fact]
        public async Task DeleteRequest_Existing_ShouldBe_Success()
        {
            var request = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync();
            var handler = new Delete.Handler(dbContext);
            var command = new Delete(request.Id);

            var response = await handler.Handle(command, new CancellationToken());

            response.Should().Be(true);
            request = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(x => x.Id == request.Id);
            request.Should().BeNull();
        }

        [Fact]
        public async Task UpdateRequest_ShouldBe_Ok()
        {
            var expectedDiscipline = "Whatever";
            var request = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync();

            var handler = new Update.Handler(profileServiceMock.Object, orgResolverMock.Object, dbContext, mediatorMock.Object);
            var command = new Update(request.Id)
                .WithDiscipline(expectedDiscipline);

            command.SetEditor(Guid.NewGuid(), null);

            await handler.Handle(command, new CancellationToken());

            request = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(x => x.Id == request.Id);
            request.Discipline.Should().Be(expectedDiscipline);
        }
    }
}
