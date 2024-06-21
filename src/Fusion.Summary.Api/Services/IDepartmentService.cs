using Fusion.Summary.Api.Database.Models;
using Fusion.Summary.Api.Database;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Services;

public interface IDepartmentService
{
    Task<DbDepartment?> GetDepartmentById(string sapId);
}

public class DepartmentService : IDepartmentService
{
    private readonly DatabaseContext _context;

    public DepartmentService(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<DbDepartment?> GetDepartmentById(string sapId)
    {
        return await _context.Departments.FindAsync(sapId);
    }
}