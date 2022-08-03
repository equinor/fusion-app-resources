using FluentAssertions;
using Fusion.ApiClients.Org;
using Fusion.Integration;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Behaviours;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fusion.Resources.Domain.Tests
{
    public class NotificationTests
    {
        public class TestHandler : INotificationHandler<SecondOpinionRequested>
        {
            public bool WasReceived { get; set; } = false;
            public Guid? SecondOpinionId { get; set; } = null;
            public Task Handle(SecondOpinionRequested notification, CancellationToken cancellationToken)
            {
                WasReceived = true;
                SecondOpinionId = notification.SecondOpinion.Id;

                return Task.CompletedTask;
            }
        }

        public class TestTrackableRequestBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
        {
            public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
            {
                if (request is ITrackableRequest trackableRequest)
                {
                    var person = NotificationTests.CreateTestPerson("Test Testesen");
                    trackableRequest.SetEditor(person.AzureUniqueId, person);
                }
                
                return await next();
            }
        }

        private readonly ServiceCollection services = new ServiceCollection();

        public NotificationTests()
        {
            services.AddMediatR(typeof(AddSecondOpinion).Assembly);
            services.AddDbContext<ResourcesDbContext>(opts => opts.UseInMemoryDatabase("NotificationTests"));

            var profileService = new Mock<IProfileService>();
            profileService
                .Setup(x => x.EnsurePersonsAsync(It.IsAny<IEnumerable<PersonId>>()))
                .ReturnsAsync(new List<DbPerson> { CreateTestPerson("Test Testesen") });
            profileService
                .Setup(x => x.EnsurePersonAsync(It.IsAny<PersonId>()))
                .ReturnsAsync(CreateTestPerson("Test Testesen"));


            services.AddSingleton(profileService.Object);
            services.AddSingleton(new Mock<IProjectOrgResolver>(MockBehavior.Loose).Object);
            services.AddSingleton(new Mock<IFusionProfileResolver>(MockBehavior.Loose).Object);
            services.AddSingleton(new Mock<IOrgApiClientFactory>(MockBehavior.Loose).Object);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TestTrackableRequestBehaviour<,>));

            services.AddLogging();
        }

        [Fact]
        public async Task SecondOpinionRequested_ShouldHaveIdSet()
        {
            services.AddSingleton<INotificationHandler<SecondOpinionRequested>, TestHandler>();
            var serviceProvider = services.BuildServiceProvider();

            var db = serviceProvider.GetRequiredService<ResourcesDbContext>();
            var proposed = CreateTestPerson("Robert C. Martin");
            var initiator = CreateTestPerson("Wobert D. Martin");
            var locationId = Guid.NewGuid();
            var project = new DbProject
            {
                Id = Guid.NewGuid(),
                OrgProjectId = Guid.NewGuid(),
                DomainId = "Project",
                Name = $"Test project {Guid.NewGuid()}"
            };

            var requestPosition = new ApiPositionV2
            {
                Id = Guid.NewGuid(),
                BasePosition = new ApiPositionBasePositionV2 { Id = Guid.NewGuid(), Department = "TPD PRD", Discipline = "IT" },
                Project = new ApiProjectReferenceV2 { ProjectId = project.OrgProjectId, DomainId = project.DomainId },
                Instances = new List<ApiPositionInstanceV2> { new ApiPositionInstanceV2 { Location = new ApiPositionLocationV2 { Id = locationId } } }
            };

            var request = new DbResourceAllocationRequest
            {
                Id = Guid.NewGuid(),
                Discipline = "IT",
                Project = project,
                OrgPositionId = requestPosition.Id,
                OrgPositionInstance = new DbResourceAllocationRequest.DbOpPositionInstance
                {
                    LocationId = locationId,
                    AppliesFrom = new DateTime(2021, 01, 01),
                    AppliesTo = new DateTime(2021, 12, 31),
                    Workload = 50
                },
                ProposedPerson = new DbResourceAllocationRequest.DbOpProposedPerson
                {
                    AzureUniqueId = proposed.AzureUniqueId,
                },
                CreatedBy = initiator
            };
            db.ResourceAllocationRequests.Add(request);
            await db.SaveChangesAsync();


            var mediator = serviceProvider.GetRequiredService<IMediator>();
            var testHandler = serviceProvider.GetRequiredService<INotificationHandler<SecondOpinionRequested>>() as TestHandler;

            var command = new AddSecondOpinion(request.Id, "Please provide your input", "description", new[] { new PersonId("lorv@equinor.com") });
            //command.SetEditor(initiator.AzureUniqueId, initiator);

            var addedSecondOpinion = await mediator.Send(command);

            testHandler.WasReceived.Should().BeTrue();
            testHandler.SecondOpinionId.Should().NotBeEmpty();
            testHandler.SecondOpinionId.Should().Be(addedSecondOpinion.Id);
        }

        private static DbPerson CreateTestPerson(string name)
        {
            return new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), Name = name, AccountType = "Employee" };
        }
    }
}
