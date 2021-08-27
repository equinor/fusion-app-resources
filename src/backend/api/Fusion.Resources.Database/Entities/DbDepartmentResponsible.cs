using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Database.Entities
{
    public class DbDepartmentResponsible
    {
        public Guid Id { get; set; }
        public string DepartmentId { get; set; } = null!;
        public Guid ResponsibleAzureObjectId { get; set; }
        public DateTimeOffset DateFrom { get; set; }
        public DateTimeOffset DateTo { get; set; }

        public DateTimeOffset DateCreated { get; set; }
        public DateTimeOffset? DateUpdated { get; set; }
        public Guid? UpdatedBy { get; set; }


        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbDepartmentResponsible>()
                .HasKey(r => r.Id);
            modelBuilder.Entity<DbDepartmentResponsible>()
                .Property(r => r.DateCreated)
                .HasDefaultValueSql("getutcdate()");
            modelBuilder.Entity<DbDepartmentResponsible>()
                .Property(r => r.DateUpdated)
                .HasDefaultValueSql("getutcdate()");
        }
    }
}
