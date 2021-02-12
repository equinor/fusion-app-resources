using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace Fusion.Resources.Database.Entities
{
    public class DbContractorRequest
    {
        public Guid Id { get; set; }
        public string? Description { get; set; }


        public DbContract Contract { get; set; } = null!;
        public Guid ContractId { get; set; }

        public DbProject Project { get; set; } = null!;
        public Guid ProjectId { get; set; }

        public DbContractPersonnel Person { get; set; } = null!;
        public Guid PersonId { get; set; }

        public RequestPosition Position { get; set; } = new RequestPosition();

        public DbRequestState State { get; set; }
        public DbRequestCategory Category { get; set; }

        public Guid? OriginalPositionId { get; set; }

        public ProvisionStatus ProvisioningStatus { get; set; } = new ProvisionStatus();

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public DbPerson CreatedBy { get; set; } = null!;
        public DbPerson? UpdatedBy { get; set; }

        public Guid CreatedById { get; set; }
        public Guid? UpdatedById { get; set; }

        public DateTimeOffset LastActivity { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbContractorRequest>(entity =>
            {
                entity.HasOne(e => e.Contract).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Project).WithMany().OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.UpdatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);


                entity.OwnsOne(e => e.ProvisioningStatus, op =>
                {
                    op.Property(ps => ps.State).HasConversion(new EnumToStringConverter<DbContractorRequest.DbProvisionState>());
                });
                entity.OwnsOne(e => e.Position, op =>
                {
                    op.OwnsOne(p => p!.TaskOwner);
                });

                entity.Property(e => e.State).HasConversion(new EnumToStringConverter<DbRequestState>());
                entity.Property(e => e.Category).HasConversion(new EnumToStringConverter<DbRequestCategory>());
                entity.Property(e => e.LastActivity).HasDefaultValue(DateTimeOffset.MinValue);
                entity.HasIndex(e => e.LastActivity).IsClustered(false);
            });
        }

        public class RequestPosition
        {
            public string? Name { get; set; } = null!;
            public Guid BasePositionId { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public string? Obs { get; set; }
            public double Workload { get; set; }

            public PositionTaskOwner TaskOwner { get; set; } = new PositionTaskOwner();
        }

        public class PositionTaskOwner
        {
            public Guid? PositionId { get; set; }
            public Guid? RequestId { get; set; }
        }

        public class ProvisionStatus
        {
            public DbProvisionState? State { get; set; } = DbProvisionState.NotProvisioned;
            public Guid? PositionId { get; set; }
            public DateTimeOffset? Provisioned { get; set; }
            public string? ErrorMessage { get; set; }
            public string? ErrorPayload { get; set; }
        }

        public enum DbProvisionState { NotProvisioned, Provisioned, Error }
    }


}
