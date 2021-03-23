using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace Fusion.Resources.Database.Entities
{
    public enum DbInternalRequestOwner { Project, ResourceOwner }

    public class DbResourceAllocationRequest
    {
        public Guid Id { get; set; }
        public string? AssignedDepartment { get;set; }
        public bool IsDraft { get; set; }

        /// <summary>
        /// The group that ownes the request. This is needed to be able to query for relevant requests. 
        /// Ex draft requests of some types should not be visible for others.
        /// </summary>
        public DbInternalRequestOwner RequestOwner { get; set; }

        public string? Discipline { get; set; }
        
        public DbInternalRequestType Type { get; set; } = DbInternalRequestType.Allocation;
        public string? SubType { get; set; }

        public DbOpState State { get; set; } = new DbOpState();

        #region Org chart relation information

        public DbProject Project { get; set; } = null!;
        public Guid ProjectId { get; set; }

        public Guid? OrgPositionId { get; set; }

        /// <summary>
        /// Cached info on the instance at the time the request was created.
        /// </summary>
        public DbOpPositionInstance OrgPositionInstance { get; set; } = new DbOpPositionInstance();

        #endregion

        public string? AdditionalNote { get; set; }
        
        /// <summary>
        /// Json serialized object with changes.
        /// </summary>
        public string? ProposedChanges { get; set; }
        public DbOpProposedPerson ProposedPerson { get; set; } = DbOpProposedPerson.Empty;

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public DbPerson CreatedBy { get; set; } = null!;
        public DbPerson? UpdatedBy { get; set; }

        public Guid CreatedById { get; set; }
        public Guid? UpdatedById { get; set; }
        public DateTimeOffset LastActivity { get; set; }

        public DbOpProvisionStatus ProvisioningStatus { get; set; } = new DbOpProvisionStatus();

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbResourceAllocationRequest>(entity =>
            {
                entity.HasOne(e => e.Project).WithMany().OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CreatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.Created);

                entity.HasOne(e => e.UpdatedBy).WithMany().OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.RequestOwner).HasConversion(new EnumToStringConverter<DbInternalRequestOwner>());

                entity.OwnsOne(e => e.ProvisioningStatus, op =>
                {
                    op.Property(ps => ps.State).HasConversion(new EnumToStringConverter<DbProvisionState>());
                });
                entity.OwnsOne(e => e.OrgPositionInstance);
                entity.OwnsOne(e => e.ProposedPerson);
                entity.OwnsOne(e => e.State);

                entity.Property(e => e.Type).HasConversion(new EnumToStringConverter<DbInternalRequestType>());
                entity.Property(e => e.LastActivity);
            });
        }

        public class DbOpPositionInstance
        {
            public Guid Id { get; set; }
            public double? Workload { get; set; }
            public string? Obs { get; set; } = null!;
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public Guid? LocationId { get; set; }
            public Guid? AssignedToUniqueId { get; set; }
            public string? AssignedToMail { get; set; }
        }

        public class DbOpProposedPerson
        {
            public bool HasBeenProposed { get; set; }
            public DateTimeOffset? ProposedAt { get; set; }
            public Guid? AzureUniqueId { get; set; }
            public string? Mail { get; set; }
            public bool WasNotified { get; set; }

            public static DbOpProposedPerson Empty => new DbOpProposedPerson() { HasBeenProposed = false };
            public void Clear()
            {
                HasBeenProposed = false;
                ProposedAt = null;
                AzureUniqueId = null;
                Mail = null;
                WasNotified = false;
            }
                
        }
    
        public class DbOpState
        {
            public string? State { get; set; }
            public bool IsCompleted { get; set; }
        }

        public class DbOpProvisionStatus
        {
            public DbProvisionState State { get; set; } = DbProvisionState.NotProvisioned;
            public Guid? OrgProjectId { get; set; }
            public Guid? OrgPositionId { get; set; }
            public Guid? OrgInstanceId { get; set; }
            public DateTimeOffset? Provisioned { get; set; }
            public string? ErrorMessage { get; set; }
            public string? ErrorPayload { get; set; }
        }
        public enum DbProvisionState { NotProvisioned, Provisioned, Error }
    }


}
