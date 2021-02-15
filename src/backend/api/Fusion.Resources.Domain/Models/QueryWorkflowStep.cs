using Fusion.Resources.Database.Entities;
using System;

#nullable enable

namespace Fusion.Resources.Domain
{
    public class QueryWorkflowStep
    {
        public QueryWorkflowStep(DbWorkflowStep step)
        {
            Id = step.Id;
            Name = step.Name;
            Description = step.Description ?? string.Empty;
            State = step.State;
            Reason = step.Reason;
            CompletedBy = QueryPerson.FromEntityOrDefault(step.CompletedBy);
            Started = step.Started;
            Completed = step.Completed;
            DueDate = step.DueDate;
            PreviousStep = step.PreviousStep;
            NextStep = step.NextStep;
        }

        public string Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string? Reason { get; set; }

        public QueryPerson? CompletedBy { get; set; }

        public DbWFStepState State { get; set; }

        public DateTimeOffset? Started { get; set; }
        public DateTimeOffset? Completed { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public string? PreviousStep { get; set; }
        public string? NextStep { get; set; }
    }
}

