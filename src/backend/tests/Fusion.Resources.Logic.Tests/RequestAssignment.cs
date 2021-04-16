using FluentAssertions;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Logic.Commands;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fusion.Resources.Logic.Tests
{
    public class RequestAssignment : IAsyncLifetime
    {
        private ResourcesDbContext db;
        private DbPerson proposed;
        private DbPerson initiator;
        private DbResourceAllocationRequest request;
        private Queries.ResolveResponsibleDepartment.Handler handler;

        public async Task InitializeAsync()
        {
            this.db = new ResourcesDbContext(
               new DbContextOptionsBuilder<ResourcesDbContext>()
               .UseInMemoryDatabase($"unit-test-db-{Guid.NewGuid()}")
               .Options
           );

            proposed = new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), Name = "Robert C. Martin" };
            initiator = new DbPerson { Id = Guid.NewGuid(), AzureUniqueId = Guid.NewGuid(), Name = "Wobert D. Martin" };

            request = new DbResourceAllocationRequest
            {
                Id = Guid.NewGuid(),
                Discipline = "IT",
                Project = new DbProject
                {
                    Id = Guid.NewGuid(),
                    DomainId = "Project"
                },
                OrgPositionInstance = new DbResourceAllocationRequest.DbOpPositionInstance
                {
                    LocationId = Guid.NewGuid(),
                    AppliesFrom = new DateTime(2021, 01, 01),
                    AppliesTo = new DateTime(2021, 12, 31),
                    Workload = 50
                },
                ProposedPerson = new DbResourceAllocationRequest.DbOpProposedPerson
                {
                    AzureUniqueId = proposed.AzureUniqueId
                }
            };

            db.Add(request);
            await db.SaveChangesAsync();

            handler = new Queries.ResolveResponsibleDepartment.Handler(db);
        }



        [Fact]
        public async Task Should_Match_On_Project_Discipline_And_Location()
        {
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
                 System.Threading.CancellationToken.None
             );

            resolvedDepartment.Should().Be(matrix.Unit);
        }

        [Fact]
        public async Task Should_Prefer_Exact_Match_When_Assigning()
        {
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
                System.Threading.CancellationToken.None
            );

            resolvedDepartment.Should().Be(exact.Unit);
        }

        [Fact]
        public async Task Should_Prefer_Discipline_Over_Location_When_Assigning()
        {
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
                System.Threading.CancellationToken.None
            );
            
            resolvedDepartment.Should().Be(exact.Unit);
        }

        

        public async  Task DisposeAsync()
        {
            await this.db.DisposeAsync();
        }
    }
}
