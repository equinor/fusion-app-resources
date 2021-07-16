using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace Fusion.Resources.Database.Entities
{
    public class DbWorkflowStep
    {
        /// <summary>
        /// Unique identifier for the step inside the workflow
        /// </summary>
        public string Id { get; set; } = null!;

        /// <summary>
        /// Display name for the step. Should be short.
        /// </summary>
        public string Name { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public string? Reason { get; set; }

        public Guid? CompletedById { get; set; }
        public DbPerson? CompletedBy { get; set; }

        public Guid WorkflowId { get; set; }
        public DbWorkflow Workflow { get; set; } = null!;

        public DbWFStepState State { get; set; }

        public DateTimeOffset? Started { get; set; }
        public DateTimeOffset? Completed { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public string? PreviousStep { get; set; }
        public string? NextStep { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbWorkflowStep>(entity =>
            {
                modelBuilder.Entity<DbWorkflowStep>(model => model.HasKey(step => new { step.Id, step.WorkflowId }));
                
                entity.Property(e => e.State).HasConversion(new EnumToStringConverter<DbWFStepState>());
                
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.PreviousStep).HasMaxLength(50);
                entity.Property(e => e.NextStep).HasMaxLength(50);

                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Reason).HasMaxLength(500);
            });
        }
    }
}
