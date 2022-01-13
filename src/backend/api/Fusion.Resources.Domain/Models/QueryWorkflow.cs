using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable
namespace Fusion.Resources.Domain
{
    public class QueryWorkflow
    {
        public QueryWorkflow(DbWorkflow workflow)
        {
            Id = workflow.Id;
            RequestId = workflow.RequestId;
            LogicAppName = workflow.LogicAppName;
            LogicAppVersion = workflow.LogicAppVersion;
            State = workflow.State;
            SystemMessage = workflow.SystemMessage;
            Created = workflow.Created;
            Completed = workflow.Completed;
            TerminatedBy = QueryPerson.FromEntityOrDefault(workflow.TerminatedBy);

            WorkflowSteps = workflow.WorkflowSteps.Select(step => new QueryWorkflowStep(step)).ToArray();
        }

        public Guid Id { get; set; }

        public Guid RequestId { get; set; }

        public string LogicAppName { get; set; }
        public string LogicAppVersion { get; set; }

        public DbWorkflowState State { get; set; }
        public string? SystemMessage { get; set; }

        public IEnumerable<QueryWorkflowStep> WorkflowSteps { get; set; }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset? Completed { get; set; }

        public QueryPerson? TerminatedBy { get; set; }

        public QueryWorkflowStep? GetWorkflowStepByState(string state)
        {
            return WorkflowSteps?.FirstOrDefault(x => string.Equals(x.Id, state, StringComparison.OrdinalIgnoreCase));
        }
    }
}

