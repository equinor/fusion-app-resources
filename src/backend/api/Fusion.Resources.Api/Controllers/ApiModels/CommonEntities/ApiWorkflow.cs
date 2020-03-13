using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiWorkflow
    {
        public ApiWorkflow(QueryWorkflow workflow)
        {
            if (workflow is null)
            {
                throw new System.ArgumentNullException(nameof(workflow));
            }

            LogicAppName = workflow.LogicAppName;
            LogicAppVersion = workflow.LogicAppVersion;

            Steps = workflow.WorkflowSteps.Select(s => new ApiWorkflowStep(s));

            State = workflow.State switch
            {
                DbWorkflowState.Running => ApiWorkflowState.Running,
                DbWorkflowState.Canceled => ApiWorkflowState.Canceled,
                DbWorkflowState.Error => ApiWorkflowState.Error,
                DbWorkflowState.Completed => ApiWorkflowState.Completed,
                DbWorkflowState.Terminated => ApiWorkflowState.Terminated,
                _ => ApiWorkflowState.Unknown,
            };
        }

        public string LogicAppName { get; set; }
        public string LogicAppVersion { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiWorkflowState State { get; set; }

        public IEnumerable<ApiWorkflowStep> Steps { get; set; }

        public enum ApiWorkflowState { Running, Canceled, Error, Completed, Terminated, Unknown }

    }

}
