using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Database.Entities;

public class DbDelegatedDepartmentResponsibleHistory
{
    public Guid Id { get; set; }
    [MaxLength(200)]
    public string DepartmentId { get; set; } = null!;
    public Guid ResponsibleAzureObjectId { get; set; }
    public DateTimeOffset DateFrom { get; set; }
    public DateTimeOffset DateTo { get; set; }
    public DateTimeOffset DateCreated { get; set; }
    public DateTimeOffset? DateUpdated { get; set; }
    public DateTimeOffset Archived { get; set; }
    public Guid? UpdatedBy { get; set; }
    public string? Reason { get; set; }


    internal static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbDelegatedDepartmentResponsibleHistory>()
            .HasKey(r => r.Id);
    }
}