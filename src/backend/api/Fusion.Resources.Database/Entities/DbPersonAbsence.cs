using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Fusion.Resources.Database.Entities
{

    public class DbPersonAbsence
    {
        public Guid Id { get; set; }
        public Guid PersonId { get; set; }
        public DbPerson Person { get; set; } = null!;
        public DateTimeOffset Created { get; set; }
        public DbPerson CreatedBy { get; set; } = null!;
        public string? Comment { get; set; }
        public DateTimeOffset AppliesFrom { get; set; }
        public DateTimeOffset? AppliesTo { get; set; }
        public DbAbsenceType Type { get; set; }
        public double? AbsencePercentage { get; set; }

        public bool IsPrivate { get; set; }
        public DbTaskDetails? TaskDetails { get; set; } = null!;

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbPersonAbsence>(entity =>
            {
                entity.Property(e => e.Type).HasConversion(new EnumToStringConverter<DbAbsenceType>());
                entity.HasOne(e => e.Person).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CreatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.OwnsOne(e => e.TaskDetails);
            });
        }
    }

    public class DbTaskDetails
    {
        public Guid? BasePositionId { get; set; }
        public string? TaskName { get; set; }
        public string RoleName { get; set; } = null!;
        public string? Location { get; set; }
    }

    public enum DbAbsenceType
    {
        Absence, Vacation, OtherTasks
    }
}
