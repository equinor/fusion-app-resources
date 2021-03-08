using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Logic.Workflows
{

    public class InternalRequestNormalWorkflowV1 : WorkflowDefinition
    {
        public const string CREATED = "created";
        public const string PROPOSAL = "proposal";
        public const string APPROVAL = "approval";
        public const string PROVISIONING = "provisioning";

        public override string Version => "v1";
        public override string Name => "Normal personnel assignment request";

        public InternalRequestNormalWorkflowV1()
            : base(null)
        {
            Steps = new List<WorkflowStep>()
            {
                Created,
                Proposal,
                Approval,
                Provisioning
            };
        }

        public InternalRequestNormalWorkflowV1(DbPerson creator)
            : this()
        {
            Step(CREATED)
                .SetDescription($"Request was created by {creator.Name}")
                .Start()
                .Complete(creator, true)
                .StartNext();
        }

        public InternalRequestNormalWorkflowV1(DbWorkflow workflow)
            : base(workflow)
        {
        }

        public WorkflowStep Approved(DbPerson approver)
        {
            return Step(APPROVAL)
                .SetName("Approved")
                .SetDescription($"{approver.Name} approved the request. The provisioning process will start so changes are visible in the org chart.")
                .Complete(approver, true)
                .StartNext().Current;
        }

        public WorkflowStep Proposed(DbPerson proposer)
        {
            return Step(PROPOSAL)
                .SetName("Proposed")
                .SetDescription($"{proposer.Name} have proposed a candidate. The project must approve the proposal for the changes to be provisioned.")
                .Complete(proposer, true)
                .StartNext().Current;
        }

        public override WorkflowStep? CompleteCurrentStep(DbWFStepState state, DbPerson user)
        {
            var current = GetCurrent();

            if (state == DbWFStepState.Rejected)
                throw new NotImplementedException("Rejected not supported");

            switch (current.Id)
            {
                case PROPOSAL:
                    return Proposed(user);

                case APPROVAL:
                    return Approved(user);
            }

            return null;
        }


        #region Step definitions

        public static WorkflowStep Created => new WorkflowStep(CREATED, "Created")
            .WithDescription("Request was created and started.")
            .WithNextStep(PROPOSAL);

        public static WorkflowStep Proposal => new WorkflowStep(PROPOSAL, "Propose")
            .WithDescription("Review personnel request and approve/reject")
            .WithPreviousStep(CREATED)
            .WithNextStep(APPROVAL);

        public static WorkflowStep Approval => new WorkflowStep(APPROVAL, "Approve")
            .WithDescription("Review personnel request and approve/reject")
            .WithPreviousStep(PROPOSAL)
            .WithNextStep(PROVISIONING);

        public static WorkflowStep Provisioning => new WorkflowStep(PROVISIONING, "Provisioning")
            .WithDescription("If the request is approved, the new position or changes will be provisioned to the organisational chart.")
            .WithPreviousStep(APPROVAL);

        #endregion
    }

}
