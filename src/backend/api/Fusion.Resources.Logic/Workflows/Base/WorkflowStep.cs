using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Logic.Workflows
{
    public class WorkflowStep
    {
        public WorkflowStep(string stepId, string name)
        {
            Id = stepId;
            Name = name;
            State = DbWFStepState.Pending;
        }

        public WorkflowStep(DbWorkflowStep step)
        {
            if (step.CompletedBy is null && step.CompletedById.HasValue)
                throw new ArgumentNullException("completedBy", "The workflow step has the completed by person set, but it is not loaded.");

            Id = step.Id;
            Name = step.Name;
            Description = step.Description;

            State = step.State;
            Completed = step.Completed;
            CompletedBy = step.CompletedBy;
            Started = step.Started;

            PreviousStepId = step.PreviousStep;
            NextStepId = step.NextStep;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Reason { get; set; }

        public string? PreviousStepId { get; set; }
        public string? NextStepId { get; set; }

        public DateTimeOffset? Completed { get; set; }
        public DbPerson? CompletedBy { get; set; }
        public DateTimeOffset? Started { get; set; }

        public DbWFStepState State { get; set; }
        

        public WorkflowStep WithName(string name)
        {
            Name = name;
            return this;
        }

        public WorkflowStep WithDescription(string description)
        {
            Description = description;
            return this;
        }
        public WorkflowStep WithPreviousStep(string id)
        {
            PreviousStepId = id;
            return this;
        }
        public WorkflowStep WithNextStep(string id)
        {
            NextStepId = id;
            return this;
        }

        public WorkflowStep Start()
        {
            Started = DateTimeOffset.Now;
            return this;
        }

        public WorkflowStep Complete(DbPerson completedBy, bool approved)
        {
            Completed = DateTimeOffset.Now;
            CompletedBy = completedBy;
            State = approved ? DbWFStepState.Approved : DbWFStepState.Rejected;
            return this;
        }

        public WorkflowStep Skip()
        {
            Completed = DateTimeOffset.Now;
            State = DbWFStepState.Skipped;
            return this;
        }


        internal DbWorkflowStep CreateDatabaseEntity()
        {
            return new DbWorkflowStep
            {
                Id = Id,
                Name = Name,
                Completed = Completed,
                CompletedBy = CompletedBy,
                Description = Description,
                DueDate = null,
                NextStep = NextStepId,
                PreviousStep = PreviousStepId,
                Reason = Reason,
                Started = Started,
                State = State
            };
        }

        internal void SaveChangesTo(DbWorkflowStep dbEntity)
        {
            dbEntity.Id = Id;
            dbEntity.Name = Name;
            dbEntity.Completed = Completed;
            dbEntity.CompletedBy = CompletedBy;
            dbEntity.Description = Description;
            dbEntity.DueDate = null;
            dbEntity.NextStep = NextStepId;
            dbEntity.PreviousStep = PreviousStepId;
            dbEntity.Reason = Reason;
            dbEntity.Started = Started;
            dbEntity.State = State;
        }
    }

}
