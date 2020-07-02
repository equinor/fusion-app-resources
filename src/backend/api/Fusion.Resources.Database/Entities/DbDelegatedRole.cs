using Microsoft.EntityFrameworkCore;
using System;

namespace Fusion.Resources.Database.Entities
{
    public class DbDelegatedRole
    {
        public Guid Id { get; set; }

        public Guid PersonId { get; set; }
        public DbPerson Person { get; set; } = null!;

        public DbDelegatedRoleType Type { get; set; }
        public DbDelegatedRoleClassification Classification { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset ValidTo { get; set; }
        public DateTimeOffset? RecertifiedDate { get; set; }

        public Guid CreatedById { get; set; }
        public DbPerson CreatedBy { get; set; } = null!;

        public Guid? RecertifiedById { get; set; }
        public DbPerson? RecertifiedBy { get; set; } = null!;

        public DbContract Contract { get; set; } = null!;
        public Guid ContractId { get; set; }

        public DbProject Project { get; set; } = null!;
        public Guid ProjectId { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbDelegatedRole>(entity =>
            {
                entity.HasOne(e => e.Contract).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Project).WithMany().OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Person).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.CreatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.RecertifiedBy).WithMany().OnDelete(DeleteBehavior.Restrict);
            });

        }
    }

}