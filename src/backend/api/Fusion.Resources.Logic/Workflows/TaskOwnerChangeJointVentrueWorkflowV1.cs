using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Logic.Workflows
{

    public class TaskOwnerChangeJointVentrueWorkflowV1 : WorkflowDefinition
    {
        public const string CREATED = "created";
        public const string APPROVAL = "approval";
        public const string PROVISIONING = "provisioning";

        public override string Version => "v1";
        public override string Name => "Joint venture personnel assignment request";

        public TaskOwnerChangeJointVentrueWorkflowV1()
            : base(null)
        {
            Steps = new List<WorkflowStep>()
            {
                Created,
                Approval,
                Provisioning
            };
        }

        public TaskOwnerChangeJointVentrueWorkflowV1(DbPerson creator)
            : this()
        {
            Step(CREATED)
                .SetDescription($"Request was created by {creator.Name}")
                .Start()
                .Complete(creator, true)
                .StartNext();
        }

        public TaskOwnerChangeJointVentrueWorkflowV1(DbWorkflow workflow)
            : base(workflow)
        {
        }

        public WorkflowStep Approved(DbPerson approver)
        {
            return Step(APPROVAL)
                .SetName("Approved")
                .SetDescription($"{approver.Name} approved the request. The provisioning process will start so the person can access resources.")
                .Complete(approver, true)
                .StartNext().Current;
        }

        public WorkflowStep SkipApproval()
        {
            return Step(APPROVAL)
                .SetName("Approved")
                .SetDescription($"No resource owner could be automatically be located, so approval step is skipped.")
                .Skip()
                .StartNext().Current;
        }

        public override WorkflowStep? CompleteCurrentStep(DbWFStepState state, DbPerson user)
        {
            var current = GetCurrent();

            if (state == DbWFStepState.Rejected)
                throw new NotImplementedException("Rejected not supported");

            switch (current.Id)
            {
                case CREATED:
                    return Approved(user);

                case PROVISIONING:
                    Step(PROVISIONING)
                        .SetName("Provisioned")
                        .SetDescription($"Changes has been published to the org chart.")
                        .Complete(user, true)
                        .CompleteWorkflow();
                    break;
            }

            return null;
        }

        #region Step definitions

        public static WorkflowStep Created => new WorkflowStep(CREATED, "Created")
            .WithDescription("Request was created and started.")
            .WithNextStep(APPROVAL);

        public static WorkflowStep Approval => new WorkflowStep(APPROVAL, "Approve")
            .WithDescription("Review personnel request and approve/reject")
            .WithPreviousStep(APPROVAL)
            .WithNextStep(PROVISIONING);

        public static WorkflowStep Provisioning => new WorkflowStep(PROVISIONING, "Provisioning")
            .WithDescription("If the request is approved, the new position or changes will be provisioned to the organisational chart.")
            .WithPreviousStep(APPROVAL);

        #endregion
    }

}
