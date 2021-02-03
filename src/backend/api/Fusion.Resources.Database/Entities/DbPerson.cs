using Microsoft.EntityFrameworkCore;
using System;

namespace Fusion.Resources.Database.Entities
{
    public class DbPerson
    {
        /// <summary>
        /// This is the local person id. Not to be confused with fusion person id, azure id etc.
        /// </summary>
        public Guid Id { get; set; }

        public Guid AzureUniqueId { get; set; }

        public string Mail { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Phone { get; set; } = null!;

        public string AccountType { get; set; } = null!;
        public string? JobTitle { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbPerson>(entity =>
            {
                entity.HasIndex(e => e.AzureUniqueId).IsUnique();
                entity.HasIndex(e => e.Mail).IsClustered(false);
            });
        }
    }

}
