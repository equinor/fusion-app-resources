﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Database.Entities
{

    public class DbExternalPersonnelPerson
    {
        public Guid Id { get; set; }

        public Guid? AzureUniqueId { get; set; }

        [MaxLength(200)]
        public string? UPN { get; set; }

        public DbAzureAccountStatus AccountStatus { get; set; }

        [MaxLength(100)]
        public string Name { get; set; } = null!;
        [MaxLength(50)]
        public string FirstName { get; set; } = null!;
        [MaxLength(50)]
        public string LastName { get; set; } = null!;

        [MaxLength(100)]
        public string Mail { get; set; } = null!;
        [MaxLength(30)]
        public string Phone { get; set; } = null!;
        [MaxLength(100)]
        public string? JobTitle { get; set; }

        [MaxLength(100)]
        public string? PreferredContractMail { get; set; }

        [MaxLength(20)]
        public string? DawinciCode { get; set; }
        [MaxLength(100)]
        public string? LinkedInProfile { get; set; }

        public bool? IsDeleted { get; set; }
        public DateTimeOffset? Deleted { get; set; }
        public ICollection<DbPersonnelDiscipline> Disciplines { get; set; } = null!;

        /// <summary>
        /// This property is used for historic person identifiers.
        /// Persons may be replaced multiple times, and can contain multiple personIds separated by comma
        /// </summary>
        public string? PersonIdReplacements { get; set; }

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
