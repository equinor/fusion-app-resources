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

        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Mail { get; set; }
        public string Phone { get; set; }
        public string JobTitle { get; set; }

        public ICollection<DbPersonnelDiscipline> Disciplines { get; set; }

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
