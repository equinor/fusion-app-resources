using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Database.Entities
{
    public class DbDepartment
    {
        public enum OrgTypes { Department, Sector };


        public Guid Id { get; set; }
        public OrgTypes OrgType { get; set; }

        public Guid? SectorId { get; set; }
        public string OrgPath { get; set; }
        public Guid Responsible { get; set; }

        public DbDepartment Sector { get; set; }
        public List<DbDepartment> Children { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbDepartment>().HasKey(dpt => dpt.Id);
            modelBuilder.Entity<DbDepartment>().HasAlternateKey(dpt => dpt.OrgPath);
        }
    }
}
