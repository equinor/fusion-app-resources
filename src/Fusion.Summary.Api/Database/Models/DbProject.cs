using Microsoft.EntityFrameworkCore;

namespace Fusion.Summary.Api.Database.Models;

public class DbProject
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }
    public required Guid OrgProjectExternalId { get; set; }


    internal static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbProject>(project =>
        {
            project.ToTable("Projects");
            project.HasKey(p => p.Id);
            project.HasIndex(p => p.OrgProjectExternalId).IsUnique();
        });
    }
}