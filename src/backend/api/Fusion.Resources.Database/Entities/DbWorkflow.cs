using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Database.Entities
{
    public class DbWorkflow
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Not really logic apps running here, but could keep the same type of properties as the query app.
        /// This should keep a name representing the logic running the workflow.
        /// </summary>
        public string LogicAppName { get; set; } = string.Empty;

        /// <summary>
        /// The logic, wherever it is implemented, should version it's logic, to give the possibility to convert/upgrade/run on old logic, as it changes.
        /// </summary>
        public string LogicAppVersion { get; set; } = string.Empty;

        public string? WorkflowClassType { get; set; }

        public DbWorkflowState State { get; set; }
        public string? SystemMessage { get; set; }

        public Guid RequestId { get; set; }
        public DbRequestType RequestType { get; set; }


        public List<DbWorkflowStep> WorkflowSteps { get; set; } = null!;

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset? Completed { get; set; }

        public DbPerson? TerminatedBy { get; set; }
        public Guid? TerminatedbyId { get; set; }


        internal static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbWorkflow>(entity =>
            {
                entity.HasIndex(e => new { e.RequestId, e.RequestType });
                entity.HasMany(e => e.WorkflowSteps).WithOne(s => s.Workflow).OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.State).HasConversion(new EnumToStringConverter<DbWorkflowState>());
            });

        }
    }
}
