using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Logic.Workflows
{
    public abstract class WorkflowDefinition
    {
        protected DbWorkflow? dbWorkflow;

        public WorkflowDefinition(DbWorkflow? dbWorkflow)
        {
            Steps = new List<WorkflowStep>();

            if (dbWorkflow != null) {

                this.dbWorkflow = dbWorkflow;

                if (dbWorkflow.WorkflowSteps == null)
                    throw new ArgumentNullException("workflow.workflowSteps", "The workflow steps must be included when initializing workflow model");

                if (dbWorkflow.LogicAppVersion != Version)
                    throw new InvalidOperationException("The workflow definition version does not match the stored workflow state. The workflow must be converted to continue.");


                Steps = dbWorkflow.WorkflowSteps
                    .Select(s => new WorkflowStep(s))
                    .ToList();

                State = dbWorkflow.State;
                SystemMessage = dbWorkflow.SystemMessage;
            }
        }

        public abstract string Name { get; }
        public abstract string Version { get; }

        public List<WorkflowStep> Steps { get; set; }
        public DbWorkflowState State { get; set; }
        public string? SystemMessage { get; set; }
        public DbPerson? TerminatedBy { get; set; }

        public WorkflowStep this[string id]
        {
            get
            {
                var step = Steps.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
                if (step == null)
                    throw new ArgumentOutOfRangeException("stepid", $"No step with id {id} located in collection");

                return step;
            }
        }

        public WorkflowStepFlow Step(string id) => new WorkflowStepFlow(this, id);

        public void SaveChanges()
        {
            if (dbWorkflow is null)
                throw new InvalidOperationException("The workflow does not yet exist.");

            if (dbWorkflow.WorkflowSteps == null)
                throw new InvalidOperationException("Workflow steps on db entity is null");

            dbWorkflow.State = State;
            dbWorkflow.TerminatedBy = TerminatedBy;
            dbWorkflow.SystemMessage = SystemMessage;

            // Remove steps not there anymore
            dbWorkflow.WorkflowSteps.RemoveAll(dbs => Steps.Any(s => dbs.Id == s.Id));

            // Update remaining 
            dbWorkflow.WorkflowSteps.ForEach(dbs =>
            {
                var step = this[dbs.Id];
                step.SaveChangesTo(dbs);
            });

            // Add new steps
            foreach (var newStep in Steps.Where(s => !dbWorkflow.WorkflowSteps.Any(dbs => dbs.Id == s.Id)))
            {
                dbWorkflow.WorkflowSteps.Add(newStep.CreateDatabaseEntity());
            }
        }

        public DbWorkflow CreateDatabaseEntity(Guid requestId, DbRequestType type)
        {
            var entity = new DbWorkflow()
            {
                Created = DateTimeOffset.Now,
                LogicAppName = Name,
                LogicAppVersion = Version,
                State = DbWorkflowState.Running,
                RequestId = requestId,
                RequestType = type,
                WorkflowSteps = Steps.Select(s => s.CreateDatabaseEntity()).ToList()
            };

            return entity;
        }
    }

}
