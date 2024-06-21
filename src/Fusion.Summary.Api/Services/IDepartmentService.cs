using Fusion.Summary.Api.Database.Entities;
using Fusion.Summary.Api.Database;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Services;

public interface IDepartmentService
{
    Task<List<DbDepartment>> GetAllDepartments();
    Task<DbDepartment?> GetDepartmentById(string sapId);
    Task <bool> CreateDepartment(DbDepartment department);
    Task UpdateDepartment(string sapId, DbDepartment department);
}

public class DepartmentService : IDepartmentService
{
    private readonly DatabaseContext _context;

    public DepartmentService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<bool> CreateDepartment(DbDepartment department)
    {
        _context.Departments.Add(department);

        await _context.SaveChangesAsync();
        
        return true;
    }

    public Task<List<DbDepartment>> GetAllDepartments()
    {
        return _context.Departments.ToListAsync();
    }

    public async Task<DbDepartment?> GetDepartmentById(string sapId)
    {
        return await _context.Departments.FindAsync(sapId);
    }

    public async Task UpdateDepartment(string sapId, DbDepartment department)
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