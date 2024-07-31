using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Database.Models;

public class DbWeeklySummaryReport
{
    public required Guid Id { get; set; }
    public required string DepartmentSapId { get; set; }

    public DbDepartment? Department { get; set; }
    public required DateTime Period { get; set; }
    public required string NumberOfPersonnel { get; set; }
    public required string CapacityInUse { get; set; }
    public required string NumberOfRequestsLastPeriod { get; set; }
    public required string NumberOfOpenRequests { get; set; }
    public required string NumberOfRequestsStartingInLessThanThreeMonths { get; set; }
    public required string NumberOfRequestsStartingInMoreThanThreeMonths { get; set; }
    public required string AverageTimeToHandleRequests { get; set; }
    public required string AllocationChangesAwaitingTaskOwnerAction { get; set; }
    public required string ProjectChangesAffectingNextThreeMonths { get; set; }

    public required List<DbEndingPosition> PositionsEnding { get; set; }
    public required List<DbPersonnelMoreThan100PercentFTE> PersonnelMoreThan100PercentFTE { get; set; }

    internal static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbWeeklySummaryReport>(report =>
        {
            report.ToTable("WeeklySummaryReports");
            report.HasKey(r => r.Id);

            report.Property(r => r.Period)
                // Strip time from date and retrieve as UTC
                .HasConversion(d => d.Date,
                    d => DateTime.SpecifyKind(d, DateTimeKind.Utc));


            report.HasIndex(r => new { r.DepartmentSapId, r.Period })
                .IsUnique();

            report.OwnsMany(r => r.PositionsEnding, pe
                => pe.ToJson("PersonnelMoreThan100PercentFTEs"));

            report.OwnsMany(r => r.PersonnelMoreThan100PercentFTE, pm
                => pm.ToJson("EndingPositions"));


            report.HasOne(r => r.Department)
                .WithMany()
                .HasForeignKey(r => r.DepartmentSapId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public class DbPersonnelMoreThan100PercentFTE
{
    public required string FullName { get; set; }
    public required int FTE { get; set; }
}

public class DbEndingPosition
{
    public required string FullName { get; set; }
    public required DateTime EndDate { get; set; }
}