using Fusion.Summary.Api.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fusion.Summary.Api.Database;

public class DatabaseContext : DbContext
{
    public DbSet<DepartmentTableEntity> Departments { get; set; }


    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }
}

