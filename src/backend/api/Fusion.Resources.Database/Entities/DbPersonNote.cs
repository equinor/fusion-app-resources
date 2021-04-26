using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Database.Entities
{
    /// <summary>
    /// The note follows the targeted person through department changes. 
    /// Considered making the note a combination of resource owner department & resource id, but this would not support changes in org structure.
    /// 
    /// With introduction of other resource pools, context properties might have to be introduced.
    /// </summary>
    public class DbPersonNote
    {
        public Guid Id { get; set; }
        public Guid AzureUniqueId { get; set; }
        [MaxLength(250)]
        public string? Title { get; set; }
        [MaxLength(2500)]
        public string? Content { get; set; }
        
        /// <summary>
        /// This flag should indicate that the note should be shared amongst other relevant resource owners.
        /// </summary>
        public bool IsShared { get; set; }
        public DateTimeOffset Updated { get; set; }

        public Guid UpdatedById { get; set; }
        public DbPerson UpdatedBy { get; set; } = null!;

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbPersonNote>(entity =>
            {
                entity.HasOne(e => e.UpdatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);

                // Create index on unique id, which will primarily be used to fetch items.
                entity.HasIndex(e => e.AzureUniqueId).IsClustered(false).IncludeProperties(e => new
                {
                    e.Id,
                    e.Title,
                    e.Content,
                    e.IsShared,
                    e.Updated,
                    e.UpdatedById
                });
            });
        }
    }
}
