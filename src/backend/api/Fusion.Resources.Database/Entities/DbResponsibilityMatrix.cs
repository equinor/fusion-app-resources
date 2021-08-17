using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Database.Entities
{


    public class DbResponsibilityMatrix
    {
        public Guid Id { get; set; }
        public DbProject? Project { get; set; } = null!;
        public Guid? LocationId { get; set; }
        [MaxLength(50)]
        public string? Discipline { get; set; }
        public Guid? BasePositionId { get; set; }
        [MaxLength(100)]
        public string? Sector { get; set; }
        [MaxLength(100)]
        public string? Unit { get; set; }
        public DbPerson? Responsible { get; set; } = null!;
        public DateTimeOffset Created { get; set; }
        public DbPerson CreatedBy { get; set; } = null!;
        public DateTimeOffset? Updated { get; set; }
        public DbPerson? UpdatedBy { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbResponsibilityMatrix>(entity =>
            {
                entity.HasOne(e => e.UpdatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CreatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Project).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Responsible).WithMany().OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Discipline);
                entity.Property(e => e.Sector);
                entity.Property(e => e.Unit);
            });

        }
    }
}
