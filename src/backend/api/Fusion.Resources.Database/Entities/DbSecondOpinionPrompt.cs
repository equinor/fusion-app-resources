using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Database.Entities
{
    public class DbSecondOpinionPrompt
    {
        public Guid Id { get; set; }
        [MaxLength(2000)]
        public string Description { get; set; } = null!;

        public List<DbSecondOpinionResponse> Responses { get; set; } = new();

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbSecondOpinionPrompt>(map =>
            {
                map.HasKey(x => x.Id);
                map.HasMany(x => x.Responses).WithOne().HasForeignKey(x => x.PromptId);
            });
        }
    }
}
