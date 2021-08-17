using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Database.Entities
{
    public class DbConversationMessage
    {
        public Guid Id { get; set; }
        [MaxLength(100)]
        public string Title { get; set; } = null!;
        [MaxLength(2000)]
        public string Body { get; set; } = null!;
        [MaxLength(60)]
        public string Category { get; set; } = null!;

        public Guid SenderId { get; set; }
        public DbPerson Sender { get; set; } = null!;

        public DbMessageRecipient Recpient { get; set; }

        public Guid RequestId { get; set; }

        public string? PropertiesJson { get; set; }
        public DateTimeOffset Sent { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbConversationMessage>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<DbConversationMessage>()
                .HasOne(x => x.Sender)
                .WithMany()
                .HasForeignKey(x => x.SenderId);

            modelBuilder.Entity<DbResourceAllocationRequest>()
                .HasMany(x => x.Conversation)
                .WithOne()
                .HasForeignKey(x => x.RequestId);

            modelBuilder.Entity<DbConversationMessage>()
                .Property(x => x.Title);
            modelBuilder.Entity<DbConversationMessage>()
               .Property(x => x.Body);
            modelBuilder.Entity<DbConversationMessage>()
               .Property(x => x.Category);

            modelBuilder.Entity<DbConversationMessage>()
                .Property(x => x.Recpient)
                .HasConversion(new EnumToStringConverter<DbMessageRecipient>())
                .HasMaxLength(60);
        }
    }

    public enum DbMessageRecipient { ResourceOwner, TaskOwner }
}
