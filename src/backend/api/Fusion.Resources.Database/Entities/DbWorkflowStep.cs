using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Resources.Database.Entities
{
    public class DbWorkflowStep
    {
        /// <summary>
        /// Unique identifier for the step inside the workflow
        /// </summary>
        [MaxLength(50)]
        public string Id { get; set; } = null!;

        /// <summary>
        /// Display name for the step. Should be short.
        /// </summary>
        [MaxLength(100)]
        public string Name { get; set; } = null!;
        [MaxLength(500)]
        public string? Description { get; set; } = null!;
        [MaxLength(500)]
        public string? Reason { get; set; }

        public Guid? CompletedById { get; set; }
        public DbPerson? CompletedBy { get; set; }

        public Guid WorkflowId { get; set; }
        public DbWorkflow Workflow { get; set; } = null!;

        public DbWFStepState State { get; set; }

        public DateTimeOffset? Started { get; set; }
        public DateTimeOffset? Completed { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        [MaxLength(50)]
        public string? PreviousStep { get; set; }
        [MaxLength(50)]
        public string? NextStep { get; set; }

        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbWorkflowStep>(entity =>
            {
                modelBuilder.Entity<DbWorkflowStep>(model => model.HasKey(step => new { step.Id, step.WorkflowId }));
                
                entity.Property(e => e.State).HasConversion(new EnumToStringConverter<DbWFStepState>());
                
                entity.Property(e => e.Id);
                entity.Property(e => e.PreviousStep);
                entity.Property(e => e.NextStep);

                entity.Property(e => e.Name);
                entity.Property(e => e.Description);
                entity.Property(e => e.Reason);
            });
        }
    }
}
