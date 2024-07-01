using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Database.Models;

public class DbSummaryReport
{
    [Key] public required Guid Id { get; set; }
    public required string DepartmentSapId { get; set; }
    public required DbSummaryReportPeriod PeriodType { get; set; }
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
        modelBuilder.Entity<DbSummaryReport>(report =>
        {
            report.HasIndex(r => r.DepartmentSapId);

            report.HasIndex(r => r.PeriodType);
            report.Property(r => r.PeriodType)
                .HasConversion<string>()
                .HasMaxLength(20);

            report.HasIndex(r => r.Period);

            report.OwnsMany(r => r.PositionsEnding);
            report.OwnsMany(r => r.PersonnelMoreThan100PercentFTE);
        });
    }
}

public class DbPersonnelMoreThan100PercentFTE
{
    public required Guid Id { get; set; }
    public required string FullName { get; set; }
    public required int FTE { get; set; }
}

public class DbEndingPosition
{
    public required Guid Id { get; set; }
    public required string FullName { get; set; }
    public required DateTime EndDate { get; set; }
}

public enum DbSummaryReportPeriod
{
    Weekly
}