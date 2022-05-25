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


        public Guid CreatedById { get; set; }
        public DbPerson CreatedBy { get; set; } = null!;

        public Guid RequestId { get; set; }
        public DbResourceAllocationRequest Request { get; set; } = null!;

        public List<DbSecondOpinionResponse> Responses { get; set; } = new();

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbSecondOpinionPrompt>(map =>
            {
                map.HasKey(x => x.Id);
                map.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById);
                map.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId);
                map.HasMany(x => x.Responses).WithOne().HasForeignKey(x => x.PromptId).OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
