using Fusion.Summary.Api.Database.Entities;
using Fusion.Summary.Api.Database;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Services;

public interface IDepartmentService
{
    Task<List<DepartmentTableEntity>> GetAllDepartments();
    Task<DepartmentTableEntity?> GetDepartmentById(string sapId);
    Task <bool> CreateDepartment(DepartmentTableEntity department);
    Task UpdateDepartment(string sapId, DepartmentTableEntity department);
}

public class DepartmentService : IDepartmentService
{
    private readonly DatabaseContext _context;

    public DepartmentService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<bool> CreateDepartment(DepartmentTableEntity department)
    {
        _context.Departments.Add(department);

        await _context.SaveChangesAsync();
        
        return true;
    }

    public Task<List<DepartmentTableEntity>> GetAllDepartments()
    {
        return _context.Departments.ToListAsync();
    }

    public async Task<DepartmentTableEntity?> GetDepartmentById(string sapId)
    {
        return await _context.Departments.FindAsync(sapId);
    }

    public async Task UpdateDepartment(string sapId, DepartmentTableEntity department)
    {
        var existingDepartment = await _context.Departments.FindAsync(sapId);

        if (existingDepartment != null)
        {
            if( existingDepartment.ResourceOwnerAzureUniqueId != department.ResourceOwnerAzureUniqueId)
            {
                existingDepartment.ResourceOwnerAzureUniqueId = department.ResourceOwnerAzureUniqueId;

                await _context.SaveChangesAsync();
            }
        }
    }
}