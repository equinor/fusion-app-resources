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

        public string CompanyName { get; set; } = null!;

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbContract>()
                .Property(c => c.CompanyName)
                .HasDefaultValue(string.Empty);
        }
    }
}
