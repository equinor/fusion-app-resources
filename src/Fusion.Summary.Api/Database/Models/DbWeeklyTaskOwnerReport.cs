using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Database.Models;

public class DbWeeklyTaskOwnerReport
{
    public required Guid Id { get; set; }
    public required Guid ProjectId { get; set; }
    public DbProject? Project { get; set; }

    public required DateTime Period { get; set; }

    internal static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbWeeklyTaskOwnerReport>(report =>
        {
            report.ToTable("WeeklyTaskOwnerReports");
            report.HasKey(r => r.Id);

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
    public required string FullName { get; set; }
    public required DateTime Expires { get; set; }
}

public class DbActionsAwaitingTaskOwner
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