using Microsoft.EntityFrameworkCore;
using System;

namespace Fusion.Resources.Database.Entities
{
    public class DbContract
    {
        public Guid Id { get; set; }
        public string ContractNumber { get; set; } = null!;
        public Guid OrgContractId { get; set; }
        public string Name { get; set; } = null!;

        public DbProject Project { get; set; } = null!;
        public Guid ProjectId { get; set; }

        public DateTimeOffset Allocated { get; set; }
        public DbPerson AllocatedBy { get; set; } = null!;
        public Guid AllocatedById { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbContract>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ContractNumber).HasMaxLength(15);
                entity.Property(e => e.Name).HasMaxLength(250);
            });
        }
    }
}