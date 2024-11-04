using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Database.Models;
using Fusion.Summary.Api.Domain.Commands;
using Fusion.Summary.Api.Domain.Models;
using Fusion.Summary.Api.Domain.Queries;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Tests.HandlerTests
{
    public class DepartmentTests : IAsyncLifetime
    {
        private SummaryDbContext _context = null!;

        private readonly DbContextOptions<SummaryDbContext> _dbContextOptions = new DbContextOptionsBuilder<SummaryDbContext>().UseInMemoryDatabase(databaseName: "test_db").Options;

        public DepartmentTests()
        {
            // Create a new instance of the SummaryDbContext
            _context = new SummaryDbContext(_dbContextOptions);
        }

        public Task InitializeAsync()
        {
            _context = new SummaryDbContext(_dbContextOptions);
            _context.Database.EnsureCreated();
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            return Task.CompletedTask;
        }

        [Fact]
        public async Task CreateDepartment_ShouldCreateDepartment()
        {
            // Arrange
            var handler = new CreateDepartment.Handler(_context);

            var department = new DbDepartment { DepartmentSapId = "1001", FullDepartmentName = "Department A" };

            // Act
            await handler.Handle(new CreateDepartment(department.DepartmentSapId, department.FullDepartmentName, [], []), CancellationToken.None);

            // Assert
            var dbDepartment = _context.Departments.FirstOrDefault(d => d.DepartmentSapId == department.DepartmentSapId);

            Assert.NotNull(dbDepartment);
        }


        [Fact]
        public async Task GetAllDepartments_ShouldReturnAllDepartments()
        {
            // Arrange
            var handler = new CreateDepartment.Handler(_context);
            var queryHandler = new GetAllDepartments.Handler(_context);

            var departmentA = new QueryDepartment { SapDepartmentId = "1001", FullDepartmentName = "Department A", ResourceOwnersAzureUniqueId = [], DelegateResourceOwnersAzureUniqueId = [] };
            var departmentB = new QueryDepartment { SapDepartmentId = "1002", FullDepartmentName = "Department B", ResourceOwnersAzureUniqueId = [], DelegateResourceOwnersAzureUniqueId = [] };

            await handler.Handle(new CreateDepartment(departmentA.SapDepartmentId, departmentA.FullDepartmentName, departmentA.ResourceOwnersAzureUniqueId, departmentA.DelegateResourceOwnersAzureUniqueId), CancellationToken.None);
            await handler.Handle(new CreateDepartment(departmentB.SapDepartmentId, departmentB.FullDepartmentName, departmentB.ResourceOwnersAzureUniqueId, departmentB.DelegateResourceOwnersAzureUniqueId), CancellationToken.None);

            // Act
            var result = await queryHandler.Handle(new GetAllDepartments(), CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.True(result.Count(x => x.SapDepartmentId == departmentA.SapDepartmentId) == 1);
            Assert.True(result.Count(x => x.SapDepartmentId == departmentB.SapDepartmentId) == 1);
        }

        [Fact]
        public async Task GetDepartmentById_ExistingId_ShouldReturnDepartment()
        {
            // Arrange
            var handler = new CreateDepartment.Handler(_context);
            var queryHandler = new GetDepartment.Handler(_context);

            var departmentA = new QueryDepartment { SapDepartmentId = "1001", FullDepartmentName = "Department A" };
            var departmentB = new QueryDepartment { SapDepartmentId = "1002", FullDepartmentName = "Department B" };

            await handler.Handle(new CreateDepartment(departmentA.SapDepartmentId, departmentA.FullDepartmentName, [], []), CancellationToken.None);
            await handler.Handle(new CreateDepartment(departmentB.SapDepartmentId, departmentB.FullDepartmentName, [], []), CancellationToken.None);

            // Act
            var result = await queryHandler.Handle(new GetDepartment(departmentA.SapDepartmentId), CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(departmentA.SapDepartmentId, result.SapDepartmentId);
        }

        [Fact]
        public async Task GetDepartmentById_NonExistingId_ShouldReturnNull()
        {
            // Arrange
            var queryHandler = new GetDepartment.Handler(_context);

            var departmentA = new QueryDepartment { SapDepartmentId = "1001", FullDepartmentName = "Department A" };

            // Act
            var result = await queryHandler.Handle(new GetDepartment(departmentA.SapDepartmentId), CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateDepartment_ExistingId_ShouldUpdateDepartment()
        {
            // Arrange
            var handler = new CreateDepartment.Handler(_context);
            var updateHandler = new UpdateDepartment.Handler(_context);
            var queryHandler = new GetDepartment.Handler(_context);

            Guid[] resourceOwners1 = [Guid.Parse("00000000-0000-0000-0000-000000000001")];
            Guid[] resourceOwners2 = [Guid.Parse("00000000-0000-0000-0000-000000000002")];

            var departmentA = new QueryDepartment { SapDepartmentId = "1001", FullDepartmentName = "Department A" };
            var departmentB = new QueryDepartment { SapDepartmentId = "1002", FullDepartmentName = "Department B" };

            await handler.Handle(new CreateDepartment(departmentA.SapDepartmentId, departmentA.FullDepartmentName, resourceOwners1, []), CancellationToken.None);

            // Act
            await updateHandler.Handle(new UpdateDepartment(departmentA.SapDepartmentId, departmentA.FullDepartmentName, resourceOwners2, []), CancellationToken.None);

            var result = await queryHandler.Handle(new GetDepartment(departmentA.SapDepartmentId), CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(resourceOwners2, result.ResourceOwnersAzureUniqueId);
        }

        [Fact]
        public async Task UpdateDepartment_NonExistingId_ShouldNotUpdateDepartment()
        {
            // Arrange
            var handler = new CreateDepartment.Handler(_context);
            var updateHandler = new UpdateDepartment.Handler(_context);
            var queryHandler = new GetDepartment.Handler(_context);

            List<Guid> resourceOwner1 = [Guid.Parse("00000000-0000-0000-0000-000000000001")];
            List<Guid> resourceOwner2 = [Guid.Parse("00000000-0000-0000-0000-000000000002")];

            var departmentA = new QueryDepartment { SapDepartmentId = "1001", FullDepartmentName = "Department A", ResourceOwnersAzureUniqueId = resourceOwner1 };
            var departmentB = new QueryDepartment { SapDepartmentId = "1002", FullDepartmentName = "Department B", ResourceOwnersAzureUniqueId = resourceOwner2 };

            await handler.Handle(new CreateDepartment(departmentA.SapDepartmentId, departmentA.FullDepartmentName, departmentA.ResourceOwnersAzureUniqueId, []), CancellationToken.None);

            // Act
            await updateHandler.Handle(new UpdateDepartment(departmentB.SapDepartmentId, departmentB.FullDepartmentName, departmentB.ResourceOwnersAzureUniqueId, []), CancellationToken.None);

            var result = await queryHandler.Handle(new GetDepartment(departmentA.SapDepartmentId), CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(resourceOwner1, result.ResourceOwnersAzureUniqueId);
        }
    }
}