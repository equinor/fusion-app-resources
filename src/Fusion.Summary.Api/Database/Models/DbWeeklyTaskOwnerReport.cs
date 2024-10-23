using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Database.Models;

public class DbWeeklyTaskOwnerReport
{
    public required Guid Id { get; set; }
    public required Guid ProjectId { get; set; }
    public DbProject? Project { get; set; }

    public required DateTime PeriodStart { get; set; }
    public required DateTime PeriodEnd { get; set; }

    //
    // Add columns
    //


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

            report.HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

// TODO: Implement the following models

public class DbAdminAccessExpiring
{
    public required Guid AzureUniqueId { get; set; }
    public required string FullName { get; set; }
    public required DateTime Expires { get; set; }
}

public class DbActionsAwaitingTaskOwners
{
    // TODO: Implement
}

public class DbPositionAllocationsEnding
{
    // TODO: Implement
}

public class DbTBNPositionsStartingSoon
{
    // TODO: Implement
}