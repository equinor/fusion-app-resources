using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Database.Entities
{
    public class DbRequestAction
    {
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string Title { get; set; } = null!;
        [MaxLength(2000)]
        public string? Body { get; set; } = null!;
        [MaxLength(60)]
        public string Type { get; set; } = null!;
        [MaxLength(60)]
        public string? SubType { get; set; }
        public DbTaskSource Source { get; set; }
        public DbTaskResponsible Responsible { get; set; }
        public bool IsResolved { get; set; } = false;
        public DateTimeOffset? ResolvedAt { get; set; }
        public Guid? ResolvedById { get; set; }
        public DbPerson? ResolvedBy { get; set; }
        public string? PropertiesJson { get; set; }

        public Guid SentById { get; set; }
        public DbPerson SentBy { get; set; } = null!;

        public Guid? AssignedToId { get; set; }
        public DbPerson? AssignedTo { get; set; }

        public bool IsRequired { get; set; } = false;

        public DateTime? DueDate { get; set; }

        public Guid RequestId { get; set; }
        public DbResourceAllocationRequest Request { get; set; } = null!;


        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbRequestAction>(builder =>
            {
                builder.HasKey(t => t.Id);
                builder.ToTable("RequestActions");

                builder
                    .HasOne(t => t.ResolvedBy)
                    .WithMany()
                    .HasForeignKey(t => t.ResolvedById);

                builder
                    .HasOne(t => t.SentBy)
                    .WithMany()
                    .HasForeignKey(t => t.SentById)
                    .IsRequired();

                builder
                    .HasOne(t => t.AssignedTo)
                    .WithMany()
                    .HasForeignKey(t => t.AssignedToId);

                builder.Property(x => x.IsRequired)
                    .HasDefaultValueSql("(0)")
                    .IsRequired();
            });

            modelBuilder.Entity<DbResourceAllocationRequest>()
                .HasMany(rq => rq.Actions)
                .WithOne(t => t.Request)
                .HasForeignKey(t => t.RequestId);

            modelBuilder.Entity<DbRequestAction>()
                .Property(x => x.Responsible)
                .HasConversion(new EnumToStringConverter<DbTaskResponsible>());

            modelBuilder.Entity<DbRequestAction>()
                .Property(x => x.Source)
                .HasConversion(new EnumToStringConverter<DbTaskSource>());
        }
    }

    public enum DbTaskSource { ResourceOwner, TaskOwner }
    public enum DbTaskResponsible { ResourceOwner, TaskOwner, Both }
}
