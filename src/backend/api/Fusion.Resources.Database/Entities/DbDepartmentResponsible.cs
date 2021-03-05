using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Database.Entities
{
    public class DbDepartmentResponsible
    {
        public Guid Id { get; set; }
        public string DepartmentId { get; set; }
        public Guid ResponsibleAzureObjectId { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbDepartmentResponsible>()
                .HasKey(r => r.Id);
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
