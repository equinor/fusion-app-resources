using Fusion.Summary.Api.Database;
using Fusion.Summary.Api.Database.Models;
using Fusion.Summary.Api.Domain.Commands;
using Fusion.Summary.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Fusion.Summary.Api.Tests;

[TestClass]
public class DepartmentServiceTests
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
        // TODO: Add dispatching tests instead of service test
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Cleanup the in-memory database after each test run
        _context.Database.EnsureDeleted();
    }

    
    [TestMethod]
    public async Task CreateDepartment_ShouldReturnTrue()
    {
        // Arrange
        var handler = new CreateDepartment.Handler(_context);

        var department = new DbDepartment { DepartmentSapId = "1001", FullDepartmentName = "Department A" };

        // Act
        var r = await handler.Handle(new CreateDepartment(department.DepartmentSapId, department.ResourceOwnerAzureUniqueId, department.FullDepartmentName));

        var result = await _departmentService.CreateDepartment(department);

        // Assert
        Assert.IsTrue(result);
    }
    /*
    [TestMethod]
    public async Task GetAllDepartments_ShouldReturnAllDepartments()
    {
        // Arrange
        var departmentA = new DbDepartment { DepartmentSapId = "1001", FullDepartmentName = "Department A" };
        var departmentB = new DbDepartment { DepartmentSapId = "1002", FullDepartmentName = "Department B" };
        await _departmentService.CreateDepartment(departmentA);
        await _departmentService.CreateDepartment(departmentB);

        // Act
        var result = await _departmentService.GetAllDepartments();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Contains(departmentA));
        Assert.IsTrue(result.Contains(departmentB));
    }

    [TestMethod]
    public async Task GetDepartmentById_ExistingId_ShouldReturnDepartment()
    {
        // Arrange
        var departmentA = new DbDepartment { DepartmentSapId = "1001", FullDepartmentName = "Department A" };
        var departmentB = new DbDepartment { DepartmentSapId = "1002", FullDepartmentName = "Department B" };
        await _departmentService.CreateDepartment(departmentA);
        await _departmentService.CreateDepartment(departmentB);

        // Act
        var result = await _departmentService.GetDepartmentById("1001");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(departmentA, result);
    }

    [TestMethod]
    public async Task GetDepartmentById_NonExistingId_ShouldReturnNull()
    {
        // Arrange

        // Act
        var result = await _departmentService.GetDepartmentById("1001");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task UpdateDepartment_ExistingId_ShouldUpdateDepartment()
    {
        // Arrange
        var resourceOwner1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var resourceOwner2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var departmentA = new DbDepartment { DepartmentSapId = "1001", FullDepartmentName = "Department A", ResourceOwnerAzureUniqueId = resourceOwner1 };
        var departmentB = new DbDepartment { DepartmentSapId = "1001", FullDepartmentName = "Department A", ResourceOwnerAzureUniqueId = resourceOwner2 };
        
        await _departmentService.CreateDepartment(departmentA);

        // Act
        await _departmentService.UpdateDepartment("1001", departmentB);
        var result = await _departmentService.GetDepartmentById("1001");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(resourceOwner2, result.ResourceOwnerAzureUniqueId);
    }

    [TestMethod]
    public async Task UpdateDepartment_NonExistingId_ShouldNotUpdateDepartment()
    {
        // Arrange
        var resourceOwner1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var resourceOwner2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var departmentA = new DbDepartment { DepartmentSapId = "1001", FullDepartmentName = "Department A", ResourceOwnerAzureUniqueId = resourceOwner1 };
        var departmentB = new DbDepartment { DepartmentSapId = "1002", FullDepartmentName = "Department B", ResourceOwnerAzureUniqueId = resourceOwner2 };
        
        await _departmentService.CreateDepartment(departmentA);

        // Act
        await _departmentService.UpdateDepartment("1002", departmentB);
        var result = await _departmentService.GetDepartmentById("1001");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(resourceOwner1, result.ResourceOwnerAzureUniqueId);
    }
    */
}