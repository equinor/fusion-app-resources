using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace Fusion.Resources.Database.Entities
{
    public class DbResourceAllocationRequest
    {
        public Guid Id { get; set; }
        public string? Discipline { get; set; }
        public DbAllocationRequestType Type { get; set; } = DbAllocationRequestType.Normal;
        public DbRequestState State { get; set; } = DbRequestState.Created;
        public DbProject Project { get; set; } = null!;
        public Guid ProjectId { get; set; }

        public Guid? OriginalPositionId { get; set; }

        public DbPositionInstance? OrgPositionInstance { get; set; } = new DbPositionInstance();

        public string? AdditionalNote { get; set; }
        public string? ProposedChanges { get; set; }
        public DbPerson? ProposedPerson { get; set; }
        public Guid? ProposedPersonId { get; set; }
        public bool? ProposedPersonWasNotified { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public DbPerson CreatedBy { get; set; } = null!;
        public DbPerson? UpdatedBy { get; set; }

        public Guid CreatedById { get; set; }
        public Guid? UpdatedById { get; set; }
        public DateTimeOffset? LastActivity { get; set; }
        public bool IsDraft { get; set; }

        public ProvisionStatus ProvisioningStatus { get; set; } = new ProvisionStatus();

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbResourceAllocationRequest>(entity =>
            {
                entity.HasOne(e => e.Project).WithMany().OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.Created).HasDefaultValueSql("getdate()");

                entity.HasOne(e => e.UpdatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);


                entity.OwnsOne(e => e.ProvisioningStatus, op =>
                {
                    op.Property(ps => ps.State).HasConversion(new EnumToStringConverter<DbProvisionState>());
                });
                entity.OwnsOne(e => e.OrgPositionInstance);

                entity.Property(e => e.Type).HasConversion(new EnumToStringConverter<DbAllocationRequestType>());
                entity.Property(e => e.State).HasConversion(new EnumToStringConverter<DbRequestState>());
                entity.Property(e => e.LastActivity).HasDefaultValueSql("getdate()");
                entity.HasIndex(e => e.LastActivity).IsClustered(false);
            });
        }

        public class DbPositionInstance
        {
            public Guid Id { get; set; }
            public double? Workload { get; set; }
            public string? Obs { get; set; } = null!;
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public Guid? LocationId { get; set; }

        }

        public class ProvisionStatus
        {
            public DbProvisionState? State { get; set; } = DbProvisionState.NotProvisioned;
            public Guid? PositionId { get; set; }
            public DateTimeOffset? Provisioned { get; set; }
            public string? ErrorMessage { get; set; }
            public string? ErrorPayload { get; set; }
        }

        public enum DbAllocationRequestType { Normal, JointVenture, Direct }
        public enum DbProvisionState { NotProvisioned, Provisioned, Error }
    }
}
