using Fusion.Integration.LineOrg;
using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Database.Models;
using Fusion.Summary.Api.Domain.Commands;
using Fusion.Summary.Api.Domain.Models;
using Fusion.Summary.Api.Domain.Queries;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Tests;

[TestClass]
public class DepartmentTests
{
    private DatabaseContext _context;
    // TODO: Add dispatching tests instead of service test

    [TestInitialize]
    public void Setup()
    {
        // Use InMemory database instead
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: "test_db").Options;

        // Create a new instance of the DatabaseContext
        _context = new DatabaseContext(options);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Cleanup the in-memory database after each test run
        _context.Database.EnsureDeleted();
    }

    [TestMethod]
    public async Task CreateDepartment_ShouldCreateDepartment()
    {
        // Arrange
        var handler = new CreateDepartment.Handler(_context);

        var department = new DbDepartment { DepartmentSapId = "1001", FullDepartmentName = "Department A" };

        // Act
        await handler.Handle(new CreateDepartment(department.DepartmentSapId, department.ResourceOwnerAzureUniqueId, department.FullDepartmentName), CancellationToken.None);

        // Assert
        var dbDepartment = _context.Departments.FirstOrDefault(d => d.DepartmentSapId == department.DepartmentSapId);

        Assert.IsNotNull(dbDepartment, "Department was not added to the database");
    }


    [TestMethod]
    public async Task GetAllDepartments_ShouldReturnAllDepartments()
    {
        // Arrange
        var handler = new CreateDepartment.Handler(_context);
        var queryHandler = new GetAllDepartments.Handler(_context);

        var departmentA = new QueryDepartment { SapDepartmentId = "1001", FullDepartmentName = "Department A", ResourceOwnerAzureUniqueId = Guid.Empty };
        var departmentB = new QueryDepartment { SapDepartmentId = "1002", FullDepartmentName = "Department B", ResourceOwnerAzureUniqueId = Guid.Empty };

        await handler.Handle(new CreateDepartment(departmentA.SapDepartmentId, departmentA.ResourceOwnerAzureUniqueId, departmentA.FullDepartmentName), CancellationToken.None);
        await handler.Handle(new CreateDepartment(departmentB.SapDepartmentId, departmentB.ResourceOwnerAzureUniqueId, departmentB.FullDepartmentName), CancellationToken.None);

        // Act
        var result = await queryHandler.Handle(new GetAllDepartments(), CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.Count(x => x.SapDepartmentId == departmentA.SapDepartmentId) == 1);
        Assert.IsTrue(result.Count(x => x.SapDepartmentId == departmentB.SapDepartmentId) == 1);
    }

    [TestMethod]
    public async Task GetDepartmentById_ExistingId_ShouldReturnDepartment()
    {
        // Arrange
        var handler = new CreateDepartment.Handler(_context);
        var queryHandler = new GetDepartment.Handler(_context);

        var departmentA = new QueryDepartment { SapDepartmentId = "1001", FullDepartmentName = "Department A" };
        var departmentB = new QueryDepartment { SapDepartmentId = "1002", FullDepartmentName = "Department B" };

        await handler.Handle(new CreateDepartment(departmentA.SapDepartmentId, departmentA.ResourceOwnerAzureUniqueId, departmentA.FullDepartmentName), CancellationToken.None);
        await handler.Handle(new CreateDepartment(departmentB.SapDepartmentId, departmentB.ResourceOwnerAzureUniqueId, departmentB.FullDepartmentName), CancellationToken.None);

        // Act
        var result = await queryHandler.Handle(new GetDepartment(departmentA.SapDepartmentId), CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(departmentA.SapDepartmentId, result.SapDepartmentId);
    }

    [TestMethod]
    public async Task GetDepartmentById_NonExistingId_ShouldReturnNull()
    {
        // Arrange
        var queryHandler = new GetDepartment.Handler(_context);

        var departmentA = new QueryDepartment { SapDepartmentId = "1001", FullDepartmentName = "Department A" };

        // Act
        var result = await queryHandler.Handle(new GetDepartment(departmentA.SapDepartmentId), CancellationToken.None);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task UpdateDepartment_ExistingId_ShouldUpdateDepartment()
    {
        // Arrange
        var handler = new CreateDepartment.Handler(_context);
        var updateHandler = new UpdateDepartment.Handler(_context);
        var queryHandler = new GetDepartment.Handler(_context);

        var resourceOwner1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var resourceOwner2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var departmentA = new QueryDepartment { SapDepartmentId = "1001", FullDepartmentName = "Department A" };
        var departmentB = new QueryDepartment { SapDepartmentId = "1002", FullDepartmentName = "Department B" };

        await handler.Handle(new CreateDepartment(departmentA.SapDepartmentId, resourceOwner1, departmentA.FullDepartmentName), CancellationToken.None);

        // Act
        await updateHandler.Handle(new UpdateDepartment(departmentA.SapDepartmentId, resourceOwner2, departmentA.FullDepartmentName), CancellationToken.None);

        var result = await queryHandler.Handle(new GetDepartment(departmentA.SapDepartmentId), CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(resourceOwner2, result.ResourceOwnerAzureUniqueId);
    }

    [TestMethod]
    public async Task UpdateDepartment_NonExistingId_ShouldNotUpdateDepartment()
    {
        // Arrange
        var handler = new CreateDepartment.Handler(_context);
        var updateHandler = new UpdateDepartment.Handler(_context);
        var queryHandler = new GetDepartment.Handler(_context);

        var resourceOwner1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var resourceOwner2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var departmentA = new QueryDepartment { SapDepartmentId = "1001", FullDepartmentName = "Department A", ResourceOwnerAzureUniqueId = resourceOwner1 };
        var departmentB = new QueryDepartment { SapDepartmentId = "1002", FullDepartmentName = "Department B", ResourceOwnerAzureUniqueId = resourceOwner2 };

        await handler.Handle(new CreateDepartment(departmentA.SapDepartmentId, departmentA.ResourceOwnerAzureUniqueId, departmentA.FullDepartmentName), CancellationToken.None);

        // Act
        await updateHandler.Handle(new UpdateDepartment(departmentB.SapDepartmentId, resourceOwner2, departmentB.FullDepartmentName), CancellationToken.None);

        var result = await queryHandler.Handle(new GetDepartment(departmentA.SapDepartmentId), CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(resourceOwner1, result.ResourceOwnerAzureUniqueId);
    }

}