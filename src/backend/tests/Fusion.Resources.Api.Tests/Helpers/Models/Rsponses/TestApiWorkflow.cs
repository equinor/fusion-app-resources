using System;
using System.Collections.Generic;

#nullable enable

namespace Fusion.Testing.Mocks
{
    public class TestApiWorkflow
    {
        public string LogicAppName { get; set; } = null!;
        public string LogicAppVersion { get; set; } = null!;

        /// <summary>
        /// Running, Canceled, Error, Completed, Terminated, Unknown
        /// </summary>
        public string State { get; set; } = null!;

        public List<TestApiWorkflowStep> Steps { get; set; } = null!;



        public class TestApiWorkflowStep
        {
            public string Id { get; set; } = null!;
            public string Name { get; set; } = null!;

            public bool IsCompleted => Completed.HasValue;

            /// <summary>
            /// Pending, Approved, Rejected, Skipped, Unknown
            /// </summary>
            public string State { get; set; }

            public DateTimeOffset? Started { get; set; }
            public DateTimeOffset? Completed { get; set; }
            public DateTimeOffset? DueDate { get; set; }
            public TestApiPerson? CompletedBy { get; set; }
            public string? Description { get; set; }
            public string? Reason { get; set; }

            public string? PreviousStep { get; set; }
            public string? NextStep { get; set; }
        }
    }
}
