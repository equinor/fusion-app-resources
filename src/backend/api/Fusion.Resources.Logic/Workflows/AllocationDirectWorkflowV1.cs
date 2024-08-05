using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Logic.Workflows
{

    public class AllocationDirectWorkflowV1 : WorkflowDefinition
    {
        public const string SUBTYPE = "direct";

        public const string CREATED = "created";
        public const string PROPOSAL = "proposal";

        public override string Version => "v1";
        public override string Name => "Direct personnel assignment request";

        public AllocationDirectWorkflowV1()
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

        public AllocationDirectWorkflowV1(DbPerson creator)
            : this()
        {
            Step(CREATED)
                .SetDescription($"Request was created by {creator.Name}")
                .Start()
                .Complete(creator, true)
                .StartNext();
        }

        public AllocationDirectWorkflowV1(DbWorkflow workflow)
            : base(workflow)
        {
        }

        public WorkflowStep AutoComplete()
        {
            return Step(PROPOSAL)
                .SetName("Proposed")
                .SetDescription("Specified resrouce resulted in auto approval of request")
                .Skip()
                .StartNext()
                .SetName("Approved")
                .SetDescription("Specified resrouce resulted in auto approval of request")
                .Skip()
                .StartNext().Current;
        }

        public WorkflowStep AutoApproveUnchangedRequest(DbPerson? completedBy = null)
        {
            // Quite hacky, but this is to avoid having to check if the request has changes in the Proposed() method.
            var approvedTheRequestText = this[PROPOSAL].Description?
                .Split("The project must approve the proposal for the changes to be provisioned.",
                    StringSplitOptions.RemoveEmptyEntries)?.FirstOrDefault();

            if (approvedTheRequestText is not null)
                this[PROPOSAL].WithDescription(approvedTheRequestText.TrimEnd() +
                                               " The request was proposed without any changes by the resource manager. The request will be auto approved.");


            return Step(APPROVAL)
                .SetName("Approved")
                .SetDescription(
                    "The request was auto approved as the request was unchanged without any proposed changes. " +
                    "The provisioning process will start so changes are visible in the org chart.")
                .Skip(completedBy)
                .StartNext().Current
                .WithDescription("The new position or changes will be provisioned to the organisational chart");
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
            .WithNextStep(PROPOSAL);

        public static WorkflowStep Proposal => new WorkflowStep(PROPOSAL, "Propose")
            .WithDescription("Review personnel request and approve/reject")
            .WithPreviousStep(CREATED)
            .WithNextStep(APPROVAL);

        public static WorkflowStep Approval => new WorkflowStep(APPROVAL, "Approve")
            .WithDescription(
                "Review personnel request and approve/reject. If there are no proposed changes, the request will be auto approved.")
            .WithPreviousStep(PROPOSAL)
            .WithNextStep(PROVISIONING);

        public static WorkflowStep Provisioning => new WorkflowStep(PROVISIONING, "Provisioning")
            .WithDescription("If the request is approved, the new position or changes will be provisioned to the organisational chart.")
            .WithPreviousStep(APPROVAL);
        
        #endregion
    }

}
