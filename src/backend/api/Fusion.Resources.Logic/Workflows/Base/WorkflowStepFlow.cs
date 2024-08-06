using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Logic.Workflows
{
    public class WorkflowStepFlow
    {
        private readonly IEnumerable<WorkflowStep> steps;
        private readonly WorkflowDefinition workflow;
        private string currentStep;

        public WorkflowStepFlow(WorkflowDefinition workflow, string currentStep)
        {
            this.steps = workflow.Steps;
            this.workflow = workflow;
            this.currentStep = currentStep;
        }

        public WorkflowStep Current => steps.First(s => s.Id == currentStep);

        public WorkflowStepFlow Start()
        {
            Current.Started = DateTimeOffset.Now;
            return this;
        }

        public WorkflowStepFlow SetDescription(string description)
        {
            Current.WithDescription(description);
            return this;
        }
        public WorkflowStepFlow SetName(string name)
        {
            Current.WithName(name);
            return this;
        }

        public WorkflowStepFlow SetReason(string reason)
        {
            Current.Reason = reason;
            return this;
        }

        public WorkflowStepFlow StartNext()
        {
            var prevStep = currentStep;
            if (NextStep() == null)
                throw new InvalidOperationException($"Cannot start next step from {prevStep}. There is no next step");

            Current.Started = DateTimeOffset.Now;
            return this;
        }

        public WorkflowStepFlow Complete(DbPerson? completedBy, bool approved)
        {
            Current.Completed = DateTimeOffset.Now;
            Current.CompletedBy = completedBy;
            Current.State = approved ? DbWFStepState.Approved : DbWFStepState.Rejected;
            return this;
        }

        public WorkflowStepFlow Skip(DbPerson? completedBy = null)
        {
            Current.Completed = DateTimeOffset.Now;
            Current.CompletedBy = completedBy;
            Current.State = DbWFStepState.Skipped;
            return this;
        }

        public WorkflowStepFlow CompleteWorkflow()
        {
            // Clear any system messages if the previous state was error;
            if (workflow.State == DbWorkflowState.Error)
            {
                workflow.SystemMessage = null;
            }

            workflow.State = DbWorkflowState.Completed;
            return this;
        }

        public WorkflowStepFlow CancelWorkflow()
        {
            workflow.State = DbWorkflowState.Canceled;
            return this;
        }
        public WorkflowStepFlow SetWorkflowError(string errorMessage)
        {
            workflow.State = DbWorkflowState.Error;
            workflow.SystemMessage = errorMessage;
            return this;
        }
        public WorkflowStepFlow SetWorkflowRunning()
        {
            workflow.State = DbWorkflowState.Running;
            workflow.SystemMessage = null;

            return this;
        }

        public WorkflowStepFlow TerminateWorkflow(DbPerson editor, string reason)
        {
            workflow.State = DbWorkflowState.Terminated;
            workflow.SystemMessage = reason;
            workflow.TerminatedBy = editor;

            return this;
        }

        public WorkflowStepFlow SkipRest()
        {
            while(NextStep() != null)
            {
                Skip();
            }

            return this;
        }

        public WorkflowStep? NextStep()
        {
            var nextId = Current.NextStepId;
            if (nextId is null)
                return null;

            currentStep = nextId;
            return Current;
        }
    }
}
