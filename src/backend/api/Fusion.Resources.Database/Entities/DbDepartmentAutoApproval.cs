using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Database.Entities
{
    public class DbDepartmentAutoApproval
    {
        public Guid Id { get; set; }
        [MaxLength(50)]
        public string DepartmentFullPath { get; set; } = null!;

        public bool IncludeSubDepartments { get; set; }
        
        /// <summary>
        /// Indicates that the auto approval feature is enabled or disabled at this level.
        /// </summary>
        public bool Enabled { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbDepartmentAutoApproval>(entity =>
            {
                entity.HasIndex(e => e.DepartmentFullPath)
                    .IncludeProperties(e => new
                    {
                        e.IncludeSubDepartments,
                        e.Enabled
                    }).IsClustered(false);
            });
        }
    }
}
