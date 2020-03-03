using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace Fusion.Resources.Database.Entities
{
    public class DbContractorRequest
    {
        public Guid Id { get; set; }
        public string Description { get; set; }


        public DbContract Contract { get; set; }
        public Guid ContractId { get; set; }

        public DbProject Project { get; set; }
        public Guid ProjectId { get; set; }

        public DbExternalPersonnelPerson Person { get; set; }

        public RequestPosition Position { get; set; } = new RequestPosition();

        public DbRequestState State { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public DbPerson CreatedBy { get; set; }
        public DbPerson UpdatedBy { get; set; }

        public Guid CreatedById { get; set; }
        public Guid? UpdatedById { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbContractorRequest>(entity =>
            {
                entity.HasOne(e => e.Contract).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Project).WithMany().OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.UpdatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);

                entity.OwnsOne(e => e.Position, op =>
                {
                    op.OwnsOne(p => p.TaskOwner);
                });

                entity.Property(e => e.State).HasConversion(new EnumToStringConverter<DbRequestState>());
            });

        }

        public class RequestPosition
        {
            public string Name { get; set; }
            public Guid BasePositionId { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public string Obs { get; set; }

            public PositionTaskOwner TaskOwner { get; set; } = new PositionTaskOwner();

            public class PositionTaskOwner
            {
                public Guid? PositionId { get; set; }
                public Guid? RequestId { get; set; }
            }
        }
    }


}
