using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Database.Models;

public class DbWeeklyTaskOwnerReport
{
    public required Guid Id { get; set; }
    public required Guid ProjectId { get; set; }
    public DbProject? Project { get; set; }

    public required DateTime PeriodStart { get; set; }
    public required DateTime PeriodEnd { get; set; }

    // Report data

    public required int ActionsAwaitingTaskOwnerAction { get; set; }
    public required List<DbAdminAccessExpiring> AdminAccessExpiringInLessThanThreeMonths { get; set; }
    public required List<DbPositionAllocationEnding> PositionAllocationsEndingInNextThreeMonths { get; set; }
    public required List<DbTBNPositionStartingSoon> TBNPositionsStartingInLessThanThreeMonths { get; set; }

    internal static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbWeeklyTaskOwnerReport>(report =>
        {
            report.ToTable("WeeklyTaskOwnerReports");
            report.HasKey(r => r.Id);

            report.HasIndex(r => new { r.ProjectId, r.PeriodStart, r.PeriodEnd })
                .IsUnique();

            report.Property(r => r.PeriodStart)
                // Strip time from date and retrieve as UTC
                .HasConversion(d => d.Date, d => DateTime.SpecifyKind(d, DateTimeKind.Utc));

            report.Property(r => r.PeriodEnd)
                // Strip time from date and retrieve as UTC
                .HasConversion(d => d.Date, d => DateTime.SpecifyKind(d, DateTimeKind.Utc));

            report.OwnsMany(r => r.AdminAccessExpiringInLessThanThreeMonths, x => x.ToJson());

            report.OwnsMany(r => r.PositionAllocationsEndingInNextThreeMonths, x => x.ToJson());

            report.OwnsMany(r => r.TBNPositionsStartingInLessThanThreeMonths, x => x.ToJson());

            report.HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}


public class DbAdminAccessExpiring
{
    public required Guid AzureUniqueId { get; set; }

    [MaxLength(120)]
    public required string FullName { get; set; }
    public required DateTime Expires { get; set; }
}

public class DbPositionAllocationEnding
{
    [MaxLength(120)]
    public required string PositionExternalId { get; set; }

    [MaxLength(120)]
    public required string PositionName { get; set; }

    [MaxLength(120)]
    public required string PositionNameDetailed { get; set; }

    public required DateTime PositionAppliesTo { get; set; }
}

public class DbTBNPositionStartingSoon
{
    [MaxLength(120)]
    public required string PositionExternalId { get; set; }

    [MaxLength(120)]
    public required string PositionName { get; set; }

    [MaxLength(120)]
    public required string PositionNameDetailed { get; set; }

    public required DateTime PositionAppliesFrom { get; set; }
}