using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Database.Entities
{

    public class DbExternalPersonnelPerson
    {
        public Guid Id { get; set; }

        public Guid? AzureUniqueId { get; set; }

        public DbAzureAccountStatus AccountStatus { get; set; }

        public string Name { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;

        public string? Mail { get; set; } = null!;
        public string? Phone { get; set; } = null!;
        public string? JobTitle { get; set; }


        public string? DawinciCode { get; set; }
        public string? LinkedInProfile { get; set; }

        public ICollection<DbPersonnelDiscipline> Disciplines { get; set; } = null!;

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbExternalPersonnelPerson>(entity =>
            {
                entity.Property(e => e.AccountStatus).HasConversion(new EnumToStringConverter<DbAzureAccountStatus>());
                entity.HasMany(e => e.Disciplines).WithOne().OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.Mail).IsClustered(false);
            });

        }
    }

}
