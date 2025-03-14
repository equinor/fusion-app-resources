using Fusion.Integration;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using Fusion.Resources.Domain.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration.Roles;

namespace Fusion.Resources.Test.Core
{
    public class DbTestFixture
    {
        protected readonly ServiceProvider serviceProvider;



        public DbTestFixture()
        {
            var services = new ServiceCollection();

            ConfigureServices(services);

            serviceProvider = services.BuildServiceProvider();
        }

        protected virtual void ConfigureServices(ServiceCollection services)
        {
            services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<DomainAssemblyMarkerType>());
            services.AddDbContext<ResourcesDbContext>(opts => opts.UseInMemoryDatabase($"unit-test-db-{Guid.NewGuid()}"));

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
            services.AddSingleton(new Mock<IFusionRolesClient>(MockBehavior.Loose).Object);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TestTrackableRequestBehaviour<,>));

            services.AddLogging();
        }

        public async Task<DbResourceAllocationRequest> AddRequest(Action<ApiPositionV2> positionSetup = null, Action<DbResourceAllocationRequest> requestSetup = null)
        {
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

            positionSetup?.Invoke(requestPosition);
            var instance = requestPosition.Instances.FirstOrDefault() ?? new ApiPositionInstanceV2
            {
                Location = new ApiPositionLocationV2 { Id = locationId },
                AppliesFrom = new DateTime(2021, 01, 01),
                AppliesTo = new DateTime(2021, 12, 31),
                Workload = 50
            };

            var request = new DbResourceAllocationRequest
            {
                Id = Guid.NewGuid(),
                Discipline = "IT",
                Project = project,
                OrgPositionId = requestPosition.Id,
                OrgPositionInstance = new DbResourceAllocationRequest.DbOpPositionInstance
                {
                    LocationId = instance.Location.Id,
                    AppliesFrom = instance.AppliesFrom,
                    AppliesTo = instance.AppliesTo,
                    Workload = instance.Workload
                },
                ProposedPerson = new DbResourceAllocationRequest.DbOpProposedPerson
                {
                    AzureUniqueId = proposed.AzureUniqueId,
                },
                CreatedBy = initiator
            };

            requestSetup?.Invoke(request);

            db.ResourceAllocationRequests.Add(request);
            await db.SaveChangesAsync();

            return request;
        }

        public static DbPerson CreateTestPerson(string name)
        {
            return new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), Name = name, AccountType = "Employee" };
        }

        class TestTrackableRequestBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
        {
            public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
            {
                if (request is ITrackableRequest trackableRequest)
                {
                    var person = DbTestFixture.CreateTestPerson("Test Testesen");
                    trackableRequest.SetEditor(person.AzureUniqueId, person);
                }

                return await next();
            }
        }
    }
}
