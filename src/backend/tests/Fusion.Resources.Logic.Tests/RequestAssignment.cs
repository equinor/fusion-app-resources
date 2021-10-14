using FluentAssertions;
using Fusion.ApiClients.Org;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fusion.Resources.Logic.Tests
{
    public class RequestAssignment : IAsyncLifetime
    {
        private ResourcesDbContext db;
        private DbPerson proposed;
        private DbPerson initiator;
        private ApiPositionV2 requestPosition;
        private DbResourceAllocationRequest request;

        public async Task InitializeAsync()
        {
            this.db = new ResourcesDbContext(
               new DbContextOptionsBuilder<ResourcesDbContext>()
               .UseInMemoryDatabase($"unit-test-db-{Guid.NewGuid()}")
               .Options
            );

            proposed = new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), Name = "Robert C. Martin" };
            initiator = new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), Name = "Wobert D. Martin" };
            var locationId = Guid.NewGuid();
            var project = new DbProject
            {
                Id = Guid.NewGuid(),
                OrgProjectId = Guid.NewGuid(),
                DomainId = "Project"
            };

            requestPosition = new ApiPositionV2
            {
                Id = Guid.NewGuid(),
                BasePosition = new ApiPositionBasePositionV2 { Id = Guid.NewGuid(), Department = "TPD PRD", Discipline = "IT" },
                Project = new ApiProjectReferenceV2 { ProjectId = project.OrgProjectId, DomainId = project.DomainId },
                Instances = new List<ApiPositionInstanceV2> { new ApiPositionInstanceV2 { Location = new ApiPositionLocationV2 { Id = locationId } } }
            };

            request = new DbResourceAllocationRequest
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
                    AzureUniqueId = proposed.AzureUniqueId
                },
            };

            db.Add(request);
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task Should_Match_On_Project_Discipline_And_Location()
        {
            var handler = CreateHandler();

            var responsible = new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), Name = "Reidun Resource Owner" };

            var matrix = new DbResponsibilityMatrix
            {
                Unit = "TPD PRD TST 123",
                Responsible = responsible,

                Project = request.Project,
                Discipline = request.Discipline,
                LocationId = request.OrgPositionInstance.LocationId
            };

            db.Add(matrix);
            await db.SaveChangesAsync();

            var resolvedDepartment = await handler.Handle(
                new Queries.ResolveResponsibleDepartment(request.Id),
                new System.Threading.CancellationToken()
            );

            resolvedDepartment.Should().Be(matrix.Unit);
        }

        [Fact]
        public async Task Should_Match_On_Project_And_Discipline()
        {
            var handler = CreateHandler();

            var responsible = new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), Name = "Reidun Resource Owner" };

            var matrix = new DbResponsibilityMatrix
            {
                Unit = "TPD PRD TST 123",
                Responsible = responsible,

                Project = request.Project,
                Discipline = request.Discipline,
                LocationId = null
            };

            db.Add(matrix);
            await db.SaveChangesAsync();

            var resolvedDepartment = await handler.Handle(
                new Queries.ResolveResponsibleDepartment(request.Id),
                 CancellationToken.None
             );

            resolvedDepartment.Should().Be(matrix.Unit);
        }

        [Fact]
        public async Task Should_Prefer_Exact_Match_When_Assigning()
        {
            var handler = CreateHandler();

            var responsible = new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), Name = "Reidun Resource Owner" };

            var exact = new DbResponsibilityMatrix
            {
                Unit = "TPD PRD TST EXCT3",
                Responsible = responsible,

                Project = request.Project,
                Discipline = request.Discipline,
                LocationId = request.OrgPositionInstance.LocationId
            };
            var approximate = new DbResponsibilityMatrix
            {
                Unit = "TPD PRD TST EXCT APPRX",
                Responsible = responsible,

                Project = request.Project,
                Discipline = request.Discipline,
                LocationId = null
            };

            db.Add(exact);
            db.Add(approximate);
            await db.SaveChangesAsync();

            var resolvedDepartment = await handler.Handle(
                new Queries.ResolveResponsibleDepartment(request.Id),
                CancellationToken.None
            );

            resolvedDepartment.Should().Be(exact.Unit);
        }

        [Fact]
        public async Task Should_Prefer_Discipline_Over_Location_When_Assigning()
        {
            var handler = CreateHandler();

            var responsible = new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), Name = "Reidun Resource Owner" };

            var exact = new DbResponsibilityMatrix
            {
                Unit = "TPD PRD TST EXCT3",
                Responsible = responsible,

                Project = request.Project,
                Discipline = request.Discipline,
                LocationId = null
            };
            var approximate = new DbResponsibilityMatrix
            {
                Unit = "TPD PRD TST EXCT APPRX",
                Responsible = responsible,

                Project = request.Project,
                Discipline = "Different Discipline",
                LocationId = request.OrgPositionInstance.LocationId
            };

            db.Add(exact);
            db.Add(approximate);
            await db.SaveChangesAsync();

            var resolvedDepartment = await handler.Handle(
                new Queries.ResolveResponsibleDepartment(request.Id),
                CancellationToken.None
            );

            resolvedDepartment.Should().Be(exact.Unit);
        }
        [Fact]
        public async Task Should_Fallback_To_BasePosition()
        {
            var position = new ApiPositionV2
            {
                Project = new ApiProjectReferenceV2 { ProjectId = request.Project.OrgProjectId },
                BasePosition = new ApiPositionBasePositionV2 { Department = "PDP PRD FE ANE" },
                Instances = new List<ApiPositionInstanceV2>()
            };

            var handler = CreateHandler(
                orgServiceMock => orgServiceMock
                    .Setup(x => x.ResolvePositionAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(position),

                mediatorMock => mediatorMock
                    .Setup(x => x.Send(It.IsAny<GetDepartment>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryDepartment(position.BasePosition.Department, null))
            );

            var resolvedDepartment = await handler.Handle(
                new Queries.ResolveResponsibleDepartment(request.Id),
                CancellationToken.None
            );

            resolvedDepartment.Should().Be(position.BasePosition.Department);
        }

        [Fact]
        public async Task Should_NotRoute_WhenNotRelevant_And_NotMatchingBaseposition()
        {
            var position = new ApiPositionV2
            {
                Project = new ApiProjectReferenceV2 { ProjectId = request.Project.Id },
                BasePosition = new ApiPositionBasePositionV2 { Department = "" },
                Instances = new List<ApiPositionInstanceV2>()
            };

            var handler = CreateHandler(
                orgServiceMock => orgServiceMock
                    .Setup(x => x.ResolvePositionAsync(It.IsAny<Guid>()))
                    .ReturnsAsync(position),

                mediatorMock => mediatorMock
                    .Setup(x => x.Send(It.IsAny<GetDepartment>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(default(QueryDepartment))
            );

            var resolvedDepartment = await handler.Handle(
                new Queries.ResolveResponsibleDepartment(request.Id),
                CancellationToken.None
            );

            resolvedDepartment.Should().Be(null);
        }

        [Fact]
        public async Task Should_Route_WhenMatchingBaseposition()
        {
            var handler = CreateHandler();

            var responsible = new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), Name = "Reidun Resource Owner" };

            var matrix = new DbResponsibilityMatrix
            {
                Unit = "ANO THER DEP ART MENT",
                Responsible = responsible,
                Project = request.Project,
                BasePositionId = requestPosition.BasePosition.Id
            };

            db.Add(matrix);
            await db.SaveChangesAsync();

            var resolvedDepartment = await handler.Handle(
                new Queries.ResolveResponsibleDepartment(request.Id),
                 CancellationToken.None
             );

            resolvedDepartment.Should().Be(matrix.Unit);
        }

        [Fact]
        public async Task Should_NotRoute_WhenLocationIsNotMatched()
        {
            var handler = CreateHandler();

            var responsible = new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), Name = "Reidun Resource Owner" };

            var matrix = new DbResponsibilityMatrix
            {
                Unit = "ANO THER DEP ART MENT",
                Responsible = responsible,
                Project = request.Project,
                LocationId = Guid.NewGuid(),
                BasePositionId = requestPosition.BasePosition.Id
            };

            db.Add(matrix);
            await db.SaveChangesAsync();

            var resolvedDepartment = await handler.Handle(
                new Queries.ResolveResponsibleDepartment(request.Id),
                 CancellationToken.None
             );

            resolvedDepartment.Should().NotBe(matrix.Unit);
        }

        [Fact]
        public async Task Should_NotPrioritizeWrongPosition_WhenRelevant()
        {
            /*
             * Request:     "prosjekt1"     "ingen lokasjon"        "project control manager
             * Regel 1:     "prosjekt2"     "null"                  "project control manager"
             * Regel 2:     "null"          "null"                  "project control manager" 
             */
            var responsible = new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), Name = "Reidun Resource Owner" };

            var position = new ApiPositionV2
            {
                Id = Guid.NewGuid(),
                Project = new ApiProjectReferenceV2 { ProjectId = request.Project.Id },
                BasePosition = new ApiPositionBasePositionV2
                {
                    Id = Guid.NewGuid(),
                    Name = "Project Control Manager",
                    Department = "PDP PRD FE ANE"
                },
                Instances = new List<ApiPositionInstanceV2> { new() }
            };

            var expectedNotMatched = new DbResponsibilityMatrix
            {
                Unit = "PDP PRD FE ANE4",
                Project = new DbProject
                {
                    Id = Guid.NewGuid(),
                    OrgProjectId = Guid.NewGuid(),
                    DomainId = "Project2"
                },
                Responsible = responsible,
                BasePositionId = position.BasePosition.Id
            };
            var expected = new DbResponsibilityMatrix
            {
                Unit = "PDP PRD FE ANE2",
                Responsible = responsible,
                BasePositionId = position.BasePosition.Id
            };

            db.Add(expected);
            db.Add(expectedNotMatched);
            await db.SaveChangesAsync();

            var myRequest = new DbResourceAllocationRequest
            {
                Id = Guid.NewGuid(),
                Discipline = "IT",
                ProjectId = request.Project.Id,
                OrgPositionId = position.Id,
                OrgPositionInstance = new DbResourceAllocationRequest.DbOpPositionInstance
                {
                    LocationId = request.OrgPositionInstance.LocationId,
                    AppliesFrom = new DateTime(2021, 01, 01),
                    AppliesTo = new DateTime(2021, 12, 31),
                    Workload = 50
                },
                ProposedPerson = new DbResourceAllocationRequest.DbOpProposedPerson
                {
                    AzureUniqueId = proposed.AzureUniqueId
                },
            };

            db.Add(myRequest);
            await db.SaveChangesAsync();

            var handler = CreateHandler(
              orgServiceMock => orgServiceMock
                  .Setup(x => x.ResolvePositionAsync(It.Is<Guid>(x => x == position.Id)))
                  .ReturnsAsync(position),
              mediatorMock => mediatorMock
                   .Setup(x => x.Send(It.Is<GetDepartment>(x => x.DepartmentId == position.BasePosition.Department), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new QueryDepartment(position.BasePosition.Department, null))
            );

            var resolvedDepartment = await handler.Handle(
                new Queries.ResolveResponsibleDepartment(myRequest.Id),
                CancellationToken.None
            );

            resolvedDepartment.Should().Be(expected.Unit);
        }
        
        [Fact]
        public async Task Should_NotMatchIrrelevantPosition()
        {
            var handler = CreateHandler();

            var badMatrix = new DbResponsibilityMatrix
            {
                Unit = "DEF NOT HERE",
                Project = request.Project,
                Discipline = request.Discipline,
            };

            var matrix = new DbResponsibilityMatrix
            {
                Unit = "TPD PRD MMS STR1",
                Project = request.Project,
            };

            db.Add(badMatrix);
            db.Add(matrix);
            await db.SaveChangesAsync();

            var resolvedDepartment = await handler.Handle(
                new Queries.ResolveResponsibleDepartment(request.Id),
                 CancellationToken.None
             );

            resolvedDepartment.Should().Be(matrix.Unit);
        }

        private Queries.ResolveResponsibleDepartment.Handler CreateHandler(
            Action<Mock<IProjectOrgResolver>> setupOrgServiceMock = null,
            Action<Mock<IMediator>> setupMediatorMock = null)
        {
            var orgServiceMock = new Mock<IProjectOrgResolver>(MockBehavior.Loose);
            orgServiceMock
                    .Setup(x => x.ResolvePositionAsync(It.Is<Guid>(x => x == requestPosition.Id)))
                    .ReturnsAsync(requestPosition);

            setupOrgServiceMock?.Invoke(orgServiceMock);

            var mediatorMock = new Mock<IMediator>(MockBehavior.Loose);
            mediatorMock
                    .Setup(x => x.Send(It.Is<GetDepartment>(x => x.DepartmentId == requestPosition.BasePosition.Department), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new QueryDepartment(requestPosition.BasePosition.Department, null));

            setupMediatorMock?.Invoke(mediatorMock);

            var profileServiceMock = new Mock<IProfileService>(MockBehavior.Loose);

            var router = new RequestRouter(db, orgServiceMock.Object, mediatorMock.Object, profileServiceMock.Object);
            return new Queries.ResolveResponsibleDepartment.Handler(db, router);
        }


        public async Task DisposeAsync()
        {
            await this.db.Database.EnsureDeletedAsync();
            await this.db.DisposeAsync();
        }
    }
}
