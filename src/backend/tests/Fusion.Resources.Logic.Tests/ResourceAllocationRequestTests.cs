using FluentAssertions;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using MediatR;
using Xunit;
using static Fusion.Resources.Logic.Commands.ResourceAllocationRequest;

namespace Fusion.Resources.Logic.Tests
{
    public class ResourceAllocationRequestTests
    {


        [Fact]
        public async Task DeleteRequest_NonExisting_ShouldBe_BadRequest()
        {
            var requestId = Guid.NewGuid();
            var dbOptions = new DbContextOptionsBuilder<ResourcesDbContext>()
                .UseInMemoryDatabase(nameof(ResourceAllocationRequestTests))
                .Options;

            var dbContext = new ResourcesDbContext(dbOptions);

            var handler = new Delete.Handler(dbContext);
            var command = new Delete(requestId);

            var response = await handler.Handle(command, new CancellationToken());
            response.Should().Be(false);
        }

        [Fact]
        public async Task DeleteRequest_Existing_ShouldBe_Success()
        {
            var dbOptions = new DbContextOptionsBuilder<ResourcesDbContext>()
                .UseInMemoryDatabase(nameof(ResourceAllocationRequestTests))
                .Options;

            var dbContext = new ResourcesDbContext(dbOptions);

            var request = new DbResourceAllocationRequest
            {
                Id = Guid.NewGuid(),
                Created = DateTime.Now,
                CreatedById = Guid.NewGuid(),
                Project = new DbProject { OrgProjectId = Guid.NewGuid() },
                State = DbResourceAllocationRequestState.Created
            };

            dbContext.Add(request);
            await dbContext.SaveChangesAsync();


            var handler = new Delete.Handler(dbContext);
            var command = new Delete(request.Id);

            var response = await handler.Handle(command, new CancellationToken());

            response.Should().Be(true);
        }

        [Fact]
        public async Task CreateRequest_ShouldBe_Ok()
        {
            var orgProject = new OrgProjectId(Guid.NewGuid());
            var orgResolverMock = new Mock<IProjectOrgResolver>();
            var project = new ApiProjectV2 { ProjectId = orgProject.ProjectId!.Value, Name = "TestProject", DomainId = "12345" };
            orgResolverMock.Setup(r => r.ResolveProjectAsync(It.IsAny<OrgProjectId>())).ReturnsAsync(project);

            var profileServiceMock = new Mock<IProfileService>();

            var mediatorMock = new Mock<IMediator>();

            var dbOptions = new DbContextOptionsBuilder<ResourcesDbContext>()
                .UseInMemoryDatabase(nameof(ResourceAllocationRequestTests))
                .Options;

            var dbContext = new ResourcesDbContext(dbOptions);

            var handler = new Create.Handler(profileServiceMock.Object, orgResolverMock.Object, dbContext, mediatorMock.Object);
            var command = new Create(project.ProjectId);
            command.SetEditor(Guid.NewGuid(), null);

            var response = await handler.Handle(command, new CancellationToken());

            response.Should().BeNull("Created item doesn't return anything");
        }

        [Fact]
        public async Task UpdateRequest_ShouldBe_Ok()
        {
            var orgProject = new OrgProjectId(Guid.NewGuid());
            var orgResolverMock = new Mock<IProjectOrgResolver>();
            var project = new ApiProjectV2 { ProjectId = orgProject.ProjectId!.Value, Name = "TestProject", DomainId = "12345" };
            orgResolverMock.Setup(r => r.ResolveProjectAsync(It.IsAny<OrgProjectId>())).ReturnsAsync(project);

            var profileServiceMock = new Mock<IProfileService>();
            var mediatorMock = new Mock<IMediator>();

            var dbOptions = new DbContextOptionsBuilder<ResourcesDbContext>()
                .UseInMemoryDatabase(nameof(ResourceAllocationRequestTests))
                .Options;

            var dbContext = new ResourcesDbContext(dbOptions);

            var request = new DbResourceAllocationRequest
            {
                Id = Guid.NewGuid(),
                Created = DateTime.Now,
                CreatedById = Guid.NewGuid(),
                Project = new DbProject { OrgProjectId = orgProject.ProjectId!.Value },
                State = DbResourceAllocationRequestState.Created
            };

            dbContext.Add(request);
            await dbContext.SaveChangesAsync();

            var handler = new Update.Handler(profileServiceMock.Object, orgResolverMock.Object, dbContext, mediatorMock.Object);
            var command = new Update(project.ProjectId, request.Id)
                .WithDiscipline("Whatever");
            
            command.SetEditor(Guid.NewGuid(), null);

            var response = await handler.Handle(command, new CancellationToken());

            response.Should().BeNull("Updated item doesn't return anything");
        }

    }
}
