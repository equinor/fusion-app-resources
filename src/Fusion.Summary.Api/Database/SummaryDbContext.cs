using Fusion.Summary.Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Database;

public class SummaryDbContext : DbContext
{
    public DbSet<DbDepartment> Departments { get; set; }
    public DbSet<DbWeeklySummaryReport> WeeklySummaryReports { get; set; }

    public DbSet<DbProject> Projects { get; set; }

    public DbSet<DbWeeklyTaskOwnerReport> WeeklyTaskOwnerReports { get; set; }


    public SummaryDbContext(DbContextOptions<SummaryDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        DbDepartment.OnModelCreating(modelBuilder);
        DbWeeklySummaryReport.OnModelCreating(modelBuilder);
        DbProject.OnModelCreating(modelBuilder);
        DbWeeklyTaskOwnerReport.OnModelCreating(modelBuilder);
    }
}

