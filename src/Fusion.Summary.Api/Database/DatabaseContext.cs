using Fusion.Summary.Api.Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fusion.Summary.Api.Database;

public class DatabaseContext : DbContext
{
    public DbSet<DbDepartment> Departments { get; set; }


    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }
}

