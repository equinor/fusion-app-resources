using MediatR;
using System;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public class CanApproveStep: INotification
        {
            public CanApproveStep(Guid RequestId, Database.Entities.DbInternalRequestType type, string currentStepId, string? nextStepId)
            {
                this.RequestId = RequestId;
                Type = type;
                CurrentStepId = currentStepId;
                NextStepId = nextStepId;
            }

            public Guid RequestId { get; }
            public Database.Entities.DbInternalRequestType Type { get; }
            public string? CurrentStepId { get; }
            public string? NextStepId { get; }
        }
    }
}