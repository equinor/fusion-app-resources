using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Database.Entities
{
    public class DbPerson
    {
        /// <summary>
        /// This is the local person id. Not to be confused with fusion person id, azure id etc.
        /// </summary>
        public Guid Id { get; set; }

        public Guid AzureUniqueId { get; set; }
        [MaxLength(100)]
        public string? Mail { get; set; } = null!;
        [MaxLength(100)]
        public string Name { get; set; } = null!;
        [MaxLength(30)]
        public string? Phone { get; set; } = null!;
        [MaxLength(100)]
        public string AccountType { get; set; } = null!;
        [MaxLength(100)]
        public string? JobTitle { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbPerson>(entity =>
            {
                entity.HasIndex(e => e.AzureUniqueId).IsUnique();
                entity.HasIndex(e => e.Mail).IsClustered(false);

                entity.Property(e => e.Name);
                entity.Property(e => e.Mail);
                entity.Property(e => e.Phone);
                entity.Property(e => e.JobTitle);
            });
        }
    }

}
