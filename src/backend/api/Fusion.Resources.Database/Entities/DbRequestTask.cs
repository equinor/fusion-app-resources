﻿using Microsoft.EntityFrameworkCore;
using System;

namespace Fusion.Resources.Database.Entities
{
    public class DbRequestTask
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string? SubType { get; set; }
        public DbTaskSource Source { get; set; }
        public DbTaskResponsible Responsible { get; set; }
        public bool IsResolved { get; set; } = false;
        public DateTimeOffset? ResolvedAt { get; set; }
        public Guid? ResolvedById { get; set; }
        public DbPerson? ResolvedBy { get; set; }
        public string PropertiesJson { get; set; } = null!;

        public Guid RequestId { get; set; }
        public DbResourceAllocationRequest Request { get; set; } = null!;


        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbRequestTask>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<DbRequestTask>()
                .HasOne(t => t.ResolvedBy)
                .WithMany()
                .HasForeignKey(t => t.ResolvedById);

            modelBuilder.Entity<DbResourceAllocationRequest>()
                .HasMany(rq => rq.Tasks)
                .WithOne(t => t.Request)
                .HasForeignKey(t => t.RequestId);
        }
    }

    public enum DbTaskSource { ResourceOwner, TaskOwner }
    public enum DbTaskResponsible { ResourceOwner, TaskOwner, Both }
}