using Fusion.Summary.Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Database;

public class DatabaseContext : DbContext
{
    public DbSet<DbDepartment> Departments { get; set; }
    public DbSet<DbSummaryReport> SummaryReports { get; set; }


    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        DbDepartment.OnModelCreating(modelBuilder);
        DbSummaryReport.OnModelCreating(modelBuilder);
    }
}

