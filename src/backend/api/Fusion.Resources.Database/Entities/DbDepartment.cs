using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Database.Entities
{
    public class DbDepartment
    {
        [MaxLength(100)]
        public string? SectorId { get; set; }
        [MaxLength(100)]
        public string DepartmentId { get; set; } = null!;

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbDepartment>(entity =>
            {
                entity.HasKey(dpt => dpt.DepartmentId);

                entity.Property(x => x.DepartmentId);
                entity.Property(x => x.SectorId);
            });

            modelBuilder.Entity<DbDepartment>().HasData(CreateSeedData());
        }

        private static List<DbDepartment> CreateSeedData()
        {
            #region SeedData
            return new List<DbDepartment>
            {
                new DbDepartment { DepartmentId = "TPD PRD FE EA", SectorId = "TPD PRD FE EA" },
                new DbDepartment { DepartmentId = "TPD PRD FE EA EA1", SectorId = "TPD PRD FE EA" },
                new DbDepartment { DepartmentId = "TPD PRD FE EA EA2", SectorId = "TPD PRD FE EA" },
                new DbDepartment { DepartmentId = "TPD PRD FE EA EA3", SectorId = "TPD PRD FE EA" },
                new DbDepartment { DepartmentId = "TPD PRD FE EA EA3 CON", SectorId = "TPD PRD FE EA" },
                new DbDepartment { DepartmentId = "TPD PRD FE EA EA4", SectorId = "TPD PRD FE EA" },
                new DbDepartment { DepartmentId = "TPD PRD FE EA EA4 CON", SectorId = "TPD PRD FE EA" },
                new DbDepartment { DepartmentId = "TPD PRD FE EA EA5", SectorId = "TPD PRD FE EA" },
                new DbDepartment { DepartmentId = "TPD PRD FE EM", SectorId = "TPD PRD FE EM" },
                new DbDepartment { DepartmentId = "TPD PRD FE EM EM1", SectorId = "TPD PRD FE EM" },
                new DbDepartment { DepartmentId = "TPD PRD FE EM EM2", SectorId = "TPD PRD FE EM" },
                new DbDepartment { DepartmentId = "TPD PRD FE EM EM3", SectorId = "TPD PRD FE EM" },
                new DbDepartment { DepartmentId = "TPD PRD FE EM EM4", SectorId = "TPD PRD FE EM" },
                new DbDepartment { DepartmentId = "TPD PRD FE EM EM5", SectorId = "TPD PRD FE EM" },
                new DbDepartment { DepartmentId = "TPD PRD FE MMS", SectorId = "TPD PRD FE MMS" },
                new DbDepartment { DepartmentId = "TPD PRD FE MMS MAT1", SectorId = "TPD PRD FE MMS" },
                new DbDepartment { DepartmentId = "TPD PRD FE MMS MAT2", SectorId = "TPD PRD FE MMS" },
                new DbDepartment { DepartmentId = "TPD PRD FE MMS MEC1", SectorId = "TPD PRD FE MMS" },
                new DbDepartment { DepartmentId = "TPD PRD FE MMS MEC2", SectorId = "TPD PRD FE MMS" },
                new DbDepartment { DepartmentId = "TPD PRD FE MMS MEC3", SectorId = "TPD PRD FE MMS" },
                new DbDepartment { DepartmentId = "TPD PRD FE MMS MEC4", SectorId = "TPD PRD FE MMS" },
                new DbDepartment { DepartmentId = "TPD PRD FE MMS STR1", SectorId = "TPD PRD FE MMS" },
                new DbDepartment { DepartmentId = "TPD PRD FE MMS STR2", SectorId = "TPD PRD FE MMS" },
                new DbDepartment { DepartmentId = "TPD PRD FE MO", SectorId = "TPD PRD FE MO" },
                new DbDepartment { DepartmentId = "TPD PRD FE MO GEO", SectorId = "TPD PRD FE MO" },
                new DbDepartment { DepartmentId = "TPD PRD FE MO GMS", SectorId = "TPD PRD FE MO" },
                new DbDepartment { DepartmentId = "TPD PRD FE MO MAP", SectorId = "TPD PRD FE MO" },
                new DbDepartment { DepartmentId = "TPD PRD FE MO MAR1", SectorId = "TPD PRD FE MO" },
                new DbDepartment { DepartmentId = "TPD PRD FE MO MAR2", SectorId = "TPD PRD FE MO" },
                new DbDepartment { DepartmentId = "TPD PRD FE SE", SectorId = "TPD PRD FE SE" },
                new DbDepartment { DepartmentId = "TPD PRD FE SE FA", SectorId = "TPD PRD FE SE" },
                new DbDepartment { DepartmentId = "TPD PRD FE SE PR1", SectorId = "TPD PRD FE SE" },
                new DbDepartment { DepartmentId = "TPD PRD FE SE PR2", SectorId = "TPD PRD FE SE" },
                new DbDepartment { DepartmentId = "TPD PRD FE SE SUS", SectorId = "TPD PRD FE SE" },
                new DbDepartment { DepartmentId = "TPD PRD FE SE TDS", SectorId = "TPD PRD FE SE" },
                new DbDepartment { DepartmentId = "TPD PRD FE SE TS", SectorId = "TPD PRD FE SE" },
                new DbDepartment { DepartmentId = "TPD PRD FE SE TWE", SectorId = "TPD PRD FE SE" },
                new DbDepartment { DepartmentId = "TPD PRD FE SP", SectorId = "TPD PRD FE SP" },
                new DbDepartment { DepartmentId = "TPD PRD FE SP SP1", SectorId = "TPD PRD FE SP" },
                new DbDepartment { DepartmentId = "TPD PRD FE SP SP2", SectorId = "TPD PRD FE SP" },
                new DbDepartment { DepartmentId = "TPD PRD FE SP SP3", SectorId = "TPD PRD FE SP" },
                new DbDepartment { DepartmentId = "TPD PRD FE SP SP4", SectorId = "TPD PRD FE SP" },
                new DbDepartment { DepartmentId = "TPD PRD FE SP SP5", SectorId = "TPD PRD FE SP" },
                new DbDepartment { DepartmentId = "TPD PRD FE SP SP6", SectorId = "TPD PRD FE SP" },
                new DbDepartment { DepartmentId = "TPD PRD PMC CCH", SectorId = "TPD PRD PMC CCH" },
                new DbDepartment { DepartmentId = "TPD PRD PMC CCH CHU1", SectorId = "TPD PRD PMC CCH" },
                new DbDepartment { DepartmentId = "TPD PRD PMC CCH CHU2", SectorId = "TPD PRD PMC CCH" },
                new DbDepartment { DepartmentId = "TPD PRD PMC CCH CHU3", SectorId = "TPD PRD PMC CCH" },
                new DbDepartment { DepartmentId = "TPD PRD PMC CCH CM1", SectorId = "TPD PRD PMC CCH" },
                new DbDepartment { DepartmentId = "TPD PRD PMC CCH CM2", SectorId = "TPD PRD PMC CCH" },
                new DbDepartment { DepartmentId = "TPD PRD PMC CCH CM3", SectorId = "TPD PRD PMC CCH" },
                new DbDepartment { DepartmentId = "TPD PRD PMC CCH CM4", SectorId = "TPD PRD PMC CCH" },
                new DbDepartment { DepartmentId = "TPD PRD PMC PCA", SectorId = "TPD PRD PMC PCA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC PCA PCA1", SectorId = "TPD PRD PMC PCA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC PCA PCA2", SectorId = "TPD PRD PMC PCA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC PCA PCA3", SectorId = "TPD PRD PMC PCA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC PCA PCA4", SectorId = "TPD PRD PMC PCA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC PCA PCA5", SectorId = "TPD PRD PMC PCA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC PCA PCA6", SectorId = "TPD PRD PMC PCA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC PCA PCA7", SectorId = "TPD PRD PMC PCA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC PM", SectorId = "TPD PRD PMC PM" },
                new DbDepartment { DepartmentId = "TPD PRD PMC PM PM1", SectorId = "TPD PRD PMC PM" },
                new DbDepartment { DepartmentId = "TPD PRD PMC PM PM2", SectorId = "TPD PRD PMC PM" },
                new DbDepartment { DepartmentId = "TPD PRD PMC PM PM3", SectorId = "TPD PRD PMC PM" },
                new DbDepartment { DepartmentId = "TPD PRD PMC PM PM4", SectorId = "TPD PRD PMC PM" },
                new DbDepartment { DepartmentId = "TPD PRD PMC QA", SectorId = "TPD PRD PMC QA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC QA ADM1", SectorId = "TPD PRD PMC QA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC QA ADM2", SectorId = "TPD PRD PMC QA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC QA ADM3", SectorId = "TPD PRD PMC QA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC QA DM2", SectorId = "TPD PRD PMC QA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC QA IDM1", SectorId = "TPD PRD PMC QA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC QA QRM1", SectorId = "TPD PRD PMC QA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC QA QRM2", SectorId = "TPD PRD PMC QA" },
                new DbDepartment { DepartmentId = "TPD PRD PMC QA RES", SectorId = "TPD PRD PMC QA" }
            };
            #endregion
        }
    }
}
