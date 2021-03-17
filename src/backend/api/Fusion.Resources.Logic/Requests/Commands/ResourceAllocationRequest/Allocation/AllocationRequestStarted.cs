using MediatR;
using System;
using Fusion.Resources.Logic.Workflows;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class Allocation
        {
            public class AllocationRequestStarted : INotification
            {
                public AllocationRequestStarted(Guid requestId, WorkflowDefinition workflow)
                {
                    RequestId = requestId;
                    Workflow = workflow;
                }

                public Guid RequestId { get; }
                public WorkflowDefinition Workflow { get; }
            }
        }
    }
}
