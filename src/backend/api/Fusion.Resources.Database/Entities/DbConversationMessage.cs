using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace Fusion.Resources.Database.Entities
{
    public class DbConversationMessage
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public string Category { get; set; } = null!;

        public Guid SenderId { get; set; }
        public DbPerson Sender { get; set; } = null!;

        public DbMessageRecipient Recpient { get; set; }

        public Guid RequestId { get; set; }

        public string? PropertiesJson { get; set; }

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
                .Property(x => x.Title).HasMaxLength(100);
            modelBuilder.Entity<DbConversationMessage>()
               .Property(x => x.Body).HasMaxLength(2000);
            modelBuilder.Entity<DbConversationMessage>()
               .Property(x => x.Category).HasMaxLength(60);

            modelBuilder.Entity<DbConversationMessage>()
                .Property(x => x.Recpient)
                .HasConversion(new EnumToStringConverter<DbMessageRecipient>());
        }
    }

    public enum DbMessageRecipient { ResourceOwner, TaskOwner, Both }
}
