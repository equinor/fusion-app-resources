using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Database.Entities
{
    public class DbSharedRequest
    {
        public Guid Id { get; set; }

        public Guid RequestId { get; set; }
        public DbResourceAllocationRequest Request { get; set; } = null!;

        public Guid SharedWithId { get; set; }
        public DbPerson SharedWith { get; set; } = null!;

        public Guid SharedById { get; set; }
        public DbPerson SharedBy { get; set; } = null!;

        [MaxLength(100)]
        public string Scope { get; set; } = null!;

        [MaxLength(100)]
        public string Source { get; set; } = null!;

        [MaxLength(1000)]
        public string Reason { get; set; } = null!;

        public bool IsRevoked { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }

        public DateTimeOffset GrantedAt { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbSharedRequest>(map =>
            {
                map.HasKey(x => x.Id);

                map.Property(x => x.IsRevoked).HasDefaultValue(false);
                map.HasIndex(x => new
                {
                    x.SharedWithId,
                    x.RequestId,
                    x.IsRevoked
                });
                map.HasQueryFilter(x => !x.IsRevoked);

                map.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId);
                map.HasOne(x => x.SharedBy).WithMany().HasForeignKey(x => x.SharedById).OnDelete(DeleteBehavior.NoAction);
                map.HasOne(x => x.SharedWith).WithMany().HasForeignKey(x => x.SharedWithId).OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
