using FluentAssertions;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
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
    public class InternalResourceAllocationRequestTests : IAsyncLifetime
    {
        private readonly Guid testOrgId = Guid.NewGuid();
        private readonly ResourcesDbContext dbContext;
        private readonly Mock<IProjectOrgResolver> orgResolverMock;
        private readonly Mock<IProfileService> profileServiceMock;
        private readonly Mock<IMediator> mediatorMock;
        private readonly ApiProjectV2 testProject;
        private Guid testRequestId = Guid.NewGuid();

        public InternalResourceAllocationRequestTests()
        {
            var dbOptions = new DbContextOptionsBuilder<ResourcesDbContext>()
                .UseInMemoryDatabase(nameof(InternalResourceAllocationRequestTests))
                .Options;

            dbContext = new ResourcesDbContext(dbOptions);

            var orgProject = new OrgProjectId(testOrgId);
            orgResolverMock = new Mock<IProjectOrgResolver>();
            testProject = new ApiProjectV2 { ProjectId = orgProject.ProjectId!.Value, Name = "TestProject", DomainId = Guid.NewGuid().ToString() };
            orgResolverMock.Setup(r => r.ResolveProjectAsync(It.IsAny<OrgProjectId>())).ReturnsAsync(testProject);
            var testPosition = new ApiPositionV2() { Id = Guid.NewGuid(), Project = new ApiProjectReferenceV2() { ProjectId = testProject.ProjectId, DomainId = testProject.DomainId, Name = testProject.Name, ProjectType = testProject.ProjectType } };
            orgResolverMock.Setup(x => x.ResolvePositionAsync(It.IsAny<Guid>())).ReturnsAsync(testPosition);

            profileServiceMock = new Mock<IProfileService>();
            var person = new DbPerson { Id = Guid.NewGuid() };
            profileServiceMock.Setup(x => x.EnsurePersonAsync(It.IsAny<PersonId>())).ReturnsAsync(person);

            mediatorMock = new Mock<IMediator>();

        }

        public async Task InitializeAsync()
        {
            var person = new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), AccountType = $"{FusionAccountType.Employee}" };
            await dbContext.Persons.AddAsync(person);

            var normalHandler = new Normal.Create.Handler(profileServiceMock.Object, orgResolverMock.Object, dbContext, mediatorMock.Object);
            var normalCommand = new Normal.Create(testProject.ProjectId)
                .WithAssignedDepartment("request.AssignedDepartment")
                .WithDiscipline("request.Discipline")
                .WithType("Normal")
                .WithProposedPerson(person.Id)
                .WithOrgPosition(Guid.NewGuid())
                .WithProposedChanges(new Dictionary<string, object>())
                .WithIsDraft(false)
                .WithAdditionalNode("request.AdditionalNote")
                .WithPositionInstance(Guid.NewGuid(),
                    DateTimeOffset.UtcNow.AddDays(-100).Date,
                    DateTimeOffset.UtcNow.AddDays(-10).Date, 60,
                    "request.OrgPositionInstance.Obs", Guid.NewGuid());
            normalCommand.SetEditor(person.Id, person);

            await normalHandler.Handle(normalCommand, CancellationToken.None);

            var jvHandler = new JointVenture.Create.Handler(profileServiceMock.Object, orgResolverMock.Object, dbContext, mediatorMock.Object);
            var jvCommand = new JointVenture.Create(testProject.ProjectId)
                .WithAssignedDepartment("request.AssignedDepartment")
                .WithDiscipline("request.Discipline")
                .WithType("JointVenture")
                .WithProposedPerson(person.Id)
                .WithOrgPosition(Guid.NewGuid())
                .WithProposedChanges(new Dictionary<string, object>())
                .WithIsDraft(false)
                .WithAdditionalNode("request.AdditionalNote")
                .WithPositionInstance(Guid.NewGuid(),
                    DateTimeOffset.UtcNow.AddDays(-100).Date,
                    DateTimeOffset.UtcNow.AddDays(-10).Date, 60,
                    "request.OrgPositionInstance.Obs", Guid.NewGuid());
            jvCommand.SetEditor(person.Id, person);

            await jvHandler.Handle(jvCommand, CancellationToken.None);


            var directHandler = new Direct.Create.Handler(profileServiceMock.Object, orgResolverMock.Object, dbContext, mediatorMock.Object);
            var directCommand = new Direct.Create(testProject.ProjectId)
                .WithAssignedDepartment("request.AssignedDepartment")
                .WithDiscipline("request.Discipline")
                .WithType("Direct")
                .WithProposedPerson(person.Id)
                .WithOrgPosition(Guid.NewGuid())
                .WithProposedChanges(new Dictionary<string, object>())
                .WithIsDraft(false)
                .WithAdditionalNode("request.AdditionalNote")
                .WithPositionInstance(Guid.NewGuid(),
                    DateTimeOffset.UtcNow.AddDays(-100).Date,
                    DateTimeOffset.UtcNow.AddDays(-10).Date, 60,
                    "request.OrgPositionInstance.Obs", Guid.NewGuid());
            directCommand.SetEditor(person.Id, person);

            await directHandler.Handle(directCommand, CancellationToken.None);

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
        public async Task DeleteRequest_Existing_ShouldBeSuccess()
        {
            var request = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(x => x.Type == DbResourceAllocationRequest.DbAllocationRequestType.Normal);
            var handler = new Delete.Handler(dbContext);
            var command = new Delete(request.Id);

            var response = await handler.Handle(command, new CancellationToken());

            response.Should().Be(true);
            request = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(x => x.Id == request.Id);
            request.Should().BeNull();
        }

        [Fact]
        public async Task UpdateRequest_TypeNormal_ShouldBeOk()
        {
            const string expectedDiscipline = "Whatever";
            var request = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(x => x.Type == DbResourceAllocationRequest.DbAllocationRequestType.Normal);
            var editor = await dbContext.Persons.FirstOrDefaultAsync();

            var handler = new Normal.Update.Handler(profileServiceMock.Object, orgResolverMock.Object, dbContext, mediatorMock.Object);
            var command = new Normal.Update(request.Id)
                .WithDiscipline(expectedDiscipline);

            command.SetEditor(editor.AzureUniqueId, editor);

            await handler.Handle(command, new CancellationToken());

            await SimplePropertyValidation(request.Id);
        }
        [Fact]
        public async Task UpdateRequest_TypeJointVenture_ShouldBeOk()
        {
            const string expectedDiscipline = "Whatever";
            var request = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(x => x.Type == DbResourceAllocationRequest.DbAllocationRequestType.JointVenture);
            var editor = await dbContext.Persons.FirstOrDefaultAsync();

            var handler = new JointVenture.Update.Handler(profileServiceMock.Object, orgResolverMock.Object, dbContext, mediatorMock.Object);
            var command = new JointVenture.Update(request.Id)
                .WithDiscipline(expectedDiscipline);

            command.SetEditor(editor.AzureUniqueId, editor);

            await handler.Handle(command, new CancellationToken());

            await SimplePropertyValidation(request.Id);
        }
        [Fact]
        public async Task UpdateRequest_TypeDirect_ShouldBeOk()
        {
            const string expectedDiscipline = "Whatever";
            var request = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(x => x.Type == DbResourceAllocationRequest.DbAllocationRequestType.Direct);
            var editor = await dbContext.Persons.FirstOrDefaultAsync();

            var handler = new Direct.Update.Handler(profileServiceMock.Object, orgResolverMock.Object, dbContext, mediatorMock.Object);
            var command = new Direct.Update(request.Id)
                .WithDiscipline(expectedDiscipline);

            command.SetEditor(editor.AzureUniqueId, editor);

            await handler.Handle(command, new CancellationToken());

            await SimplePropertyValidation(request.Id);
        }

        private async Task SimplePropertyValidation(Guid requestId)
        {
            var request = await dbContext.ResourceAllocationRequests.FirstOrDefaultAsync(x => x.Id == requestId);
            request.Id.Should().NotBeEmpty();
            request.Discipline.Should().NotBeEmpty();
            request.Type.Should().NotBeNull();
            request.State.Should().NotBeNull();
            request.ProjectId.Should().NotBeEmpty();
            request.OrgPositionId.Should().NotBeEmpty();
            request.OrgPositionInstance.Should().NotBeNull();
            request.OrgPositionInstance.AppliesFrom.Should().BeBefore(DateTime.UtcNow);
            request.OrgPositionInstance.AppliesTo.Should().BeBefore(DateTime.UtcNow);
            request.OrgPositionInstance.LocationId.Should().NotBeNull();
            request.OrgPositionInstance.Obs.Should().NotBeNull();
            request.OrgPositionInstance.Workload.Should().NotBeNull();
            request.AdditionalNote.Should().NotBeNullOrEmpty();
            request.ProposedChanges.Should().NotBeNullOrEmpty();
            request.ProposedPersonId.Should().NotBeNull();
            request.ProposedPersonWasNotified.Should().NotBeNull();
            request.Created.Should().NotBeAfter(request.Updated.GetValueOrDefault(DateTimeOffset.MaxValue));
            request.Updated.Should().NotBeAfter(DateTimeOffset.UtcNow);
            request.CreatedById.Should().NotBeEmpty();
            request.UpdatedById.Should().NotBeEmpty();
            request.LastActivity.Should().Be(request.Updated.GetValueOrDefault(DateTimeOffset.MaxValue));
            request.IsDraft.Should().BeFalse();
            request.ProvisioningStatus.Should().NotBeNull();
            request.AssignedDepartment.Should().NotBeNullOrEmpty();
        }
    }
}
