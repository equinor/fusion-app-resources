using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace Fusion.Resources.Database.Entities
{
    public class DbRequestComment
    {
        public Guid Id { get; set; }

        public string Comment { get; set; } = null!;

        public DbOrigin Origin { get; set; }

        public DateTimeOffset Created { get; set; }

        public Guid CreatedById { get; set; }

        public DbPerson CreatedBy { get; set; } = null!;

        public DateTimeOffset? Updated { get; set; }

        public Guid? UpdatedById { get; set; }

        public DbPerson? UpdatedBy { get; set; } = null!;

        /// <summary>
        /// Non-FK identifier for request.
        /// </summary>
        public Guid RequestId { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbRequestComment>(c => c.HasIndex(p => p.RequestId).IsClustered(false));
            modelBuilder.Entity<DbRequestComment>(c => c.Property(e => e.Origin).HasConversion(new EnumToStringConverter<DbOrigin>()));
        }

        public enum DbOrigin { Unknown, Company, Contractor, Local, Application }
    }
}
