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
        public DateTimeOffset DateUpdated { get; set; }
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

            modelBuilder.Entity<DbDepartmentResponsible>()
                .HasData(CreateSeedData());
        }

        private static List<DbDepartmentResponsible> CreateSeedData()
        {
            #region SeedData
            return new List<DbDepartmentResponsible>
            {
                new DbDepartmentResponsible
                {
                    Id = new Guid("20621FBC-DC4E-4958-95C9-2AC56E166973"),
                    DepartmentId = "TPD PRD PMC PCA PCA7",
                    ResponsibleAzureObjectId = new Guid("20621FBC-DC4E-4958-95C9-2AC56E166973"),
                    DateFrom = new DateTime(2020, 12, 24),
                    DateTo = new DateTime(2021, 12, 31)
                }
            };
            #endregion
        }
    }
}
