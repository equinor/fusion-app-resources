using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Database.Entities
{
    public enum DbSecondOpinionResponseStates { Open, Draft, Published }
    public class DbSecondOpinionResponse
    {
        public Guid Id { get; set; }
        public Guid PromptId { get; set; }
        public DbSecondOpinionPrompt SecondOpinion { get; set; } = null!;

        public Guid AssignedToId { get; set; }
        public DbPerson AssignedTo { get; set; } = null!;


        public DateTimeOffset? AnsweredAt { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }
        public DbSecondOpinionResponseStates State { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbSecondOpinionResponse>(map =>
            {
                map.ToTable("SecondOpinionResponses");
                map.HasKey(x => x.Id);
                map.HasOne(x => x.AssignedTo).WithMany().HasForeignKey(x => x.AssignedToId);
                
                map.Property(x => x.State)
                    .HasConversion(new EnumToStringConverter<DbSecondOpinionResponseStates>())
                    .HasMaxLength(64);
            });
        }
    }
}