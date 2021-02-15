using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Controllers
{

    public class ApiWorkflowStep
    {
        public ApiWorkflowStep(QueryWorkflowStep step)
        {
            Id = step.Id;
            Name = step.Name;
            Started = step.Started;
            Completed = step.Completed;
            DueDate = step.DueDate;
            CompletedBy = ApiPerson.FromEntityOrDefault(step.CompletedBy);
            PreviousStep = step.PreviousStep;
            NextStep = step.NextStep;
            Description = step.Description;
            Reason = step.Reason;

            State = step.State switch
            {
                DbWFStepState.Approved => ApiWorkflowStepState.Approved,
                DbWFStepState.Pending => ApiWorkflowStepState.Pending,
                DbWFStepState.Rejected => ApiWorkflowStepState.Rejected,
                DbWFStepState.Skipped => ApiWorkflowStepState.Skipped,
                _ => ApiWorkflowStepState.Unknown,
            };
        }

        public string Id { get; set; }
        public string Name { get; set; }

        public bool IsCompleted => Completed.HasValue;

        /// <summary>
        /// Pending, Approved, Rejected, Skipped
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiWorkflowStepState State { get; set; }

        public DateTimeOffset? Started { get; set; }
        public DateTimeOffset? Completed { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public ApiPerson? CompletedBy { get; set; }
        public string Description { get; set; }
        public string? Reason { get; set; }

        public string? PreviousStep { get; set; }
        public string? NextStep { get; set; }

        public enum ApiWorkflowStepState { Pending, Approved, Rejected, Skipped, Unknown }
    }

}
