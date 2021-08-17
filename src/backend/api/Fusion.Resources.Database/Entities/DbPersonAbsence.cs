using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Database.Entities
{

    public class DbPersonAbsence
    {
        public Guid Id { get; set; }
        public Guid PersonId { get; set; }
        public DbPerson Person { get; set; } = null!;
        public DateTimeOffset Created { get; set; }
        public DbPerson CreatedBy { get; set; } = null!;
        [MaxLength(5000)]
        public string? Comment { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public DbAbsenceType Type { get; set; }
        public double? AbsencePercentage { get; set; }

        public bool IsPrivate { get; set; }
        public DbOpTaskDetails? TaskDetails { get; set; } = null!;

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbPersonAbsence>(entity =>
            {
                entity.Property(e => e.Type).HasConversion(new EnumToStringConverter<DbAbsenceType>());
                entity.Property(e => e.Comment);

                entity.HasOne(e => e.Person).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CreatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.OwnsOne(e => e.TaskDetails, op =>
                {
                    op.Property(x => x!.TaskName);
                    op.Property(x => x!.RoleName);
                    op.Property(x => x!.Location);
                });
            });
        }
    }

    public class DbOpTaskDetails
    {
        public Guid? BasePositionId { get; set; }
        [MaxLength(100)]
        public string? TaskName { get; set; }
        [MaxLength(100)]
        public string RoleName { get; set; } = null!;
        [MaxLength(50)]
        public string? Location { get; set; }
    }

    public enum DbAbsenceType
    {
        Absence, Vacation, OtherTasks
    }
}
