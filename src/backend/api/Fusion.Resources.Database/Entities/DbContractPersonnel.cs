using Microsoft.EntityFrameworkCore;
using System;

namespace Fusion.Resources.Database.Entities
{

    public class DbContractPersonnel : ITrackableEntity
    {
        public Guid Id { get; set; }

        public DbContract Contract { get; set; }
        public Guid ContractId { get; set; }

        public DbProject Project { get; set; }
        public Guid ProjectId { get; set; }

        public DbExternalPersonnelPerson Person { get; set; }
        public Guid PersonId { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public DbPerson CreatedBy { get; set; }
        public DbPerson UpdatedBy { get; set; }

        public Guid CreatedById { get; set; }
        public Guid? UpdatedById { get; set; }


        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbContractPersonnel>(entity =>
            {
                entity.HasOne(e => e.Contract).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Project).WithMany().OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.UpdatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);
            });

        }
    }

}
