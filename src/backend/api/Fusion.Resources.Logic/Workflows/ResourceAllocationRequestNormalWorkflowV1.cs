using Fusion.Resources.Database.Entities;
using System.Collections.Generic;

namespace Fusion.Resources.Logic.Workflows
{

    public class ResourceAllocationRequestNormalWorkflowV1 : WorkflowDefinition
    {
        public const string CREATED = "created";
        public const string COMPANY_PROPOSAL = "companyProposal";
        public const string COMPANY_APPROVAL = "companyApproval";
        public const string PROVISIONING = "provisioning";

        public override string Version => "v1";
        public override string Name => "Contractor personnel request";

        public ResourceAllocationRequestNormalWorkflowV1()
            : base(null)
        {
            Steps = new List<WorkflowStep>()
            {
                Created,
                CompanyProposal,
                CompanyApproval,
                Provisioning
            };
        }

        public ResourceAllocationRequestNormalWorkflowV1(DbPerson creator)
            : this()
        {
            Step(CREATED)
                .SetDescription($"Request was created by {creator.Name}")
                .Start()
                .Complete(creator, true)
                .StartNext();
        }

        public ResourceAllocationRequestNormalWorkflowV1(DbWorkflow workflow)
            : base(workflow)
        {
        }

        public void CompanyApproved(DbPerson approver)
        {
            Step(COMPANY_APPROVAL)
                .SetName("Approved")
                .SetDescription($"{approver.Name} approved the request. The provisioing process will start so the person can access resources.")
                .Complete(approver, true)
                .StartNext();
        }

        public void CompanyProposed(DbPerson proposer)
        {
            Step(COMPANY_APPROVAL)
                .SetName("Approved")
                .SetDescription($"{proposer.Name} approved the request. The provisioing process will start so the person can access resources.")
                .Complete(proposer, true)
                .StartNext();
        }

        public void CompanyRejected(DbPerson editor, string reason)
        {
            Step(COMPANY_APPROVAL)
                .SetName("Submitted")
                .SetDescription($"{editor.Name} rejected the request.")
                .SetReason(reason)
                .Complete(editor, false)
                .SkipRest()
                .CompleteWorkflow();
        }

        #region Step definitions

        public static WorkflowStep Created => new WorkflowStep(CREATED, "Created")
            .WithDescription("Request was created and started.")
            .WithNextStep(COMPANY_PROPOSAL);

        public static WorkflowStep CompanyProposal => new WorkflowStep(COMPANY_PROPOSAL, "Propose")
            .WithDescription("Review personnel request and approve/reject")
            .WithPreviousStep(CREATED)
            .WithNextStep(COMPANY_APPROVAL);

        public static WorkflowStep CompanyApproval => new WorkflowStep(COMPANY_APPROVAL, "Approve")
            .WithDescription("Review personnel request and approve/reject")
            .WithPreviousStep(COMPANY_PROPOSAL)
            .WithNextStep(PROVISIONING);

        public static WorkflowStep Provisioning => new WorkflowStep(PROVISIONING, "Provisioning")
            .WithDescription("If the request is approved, the new position or changes will be provisioned to the organisational chart.")
            .WithPreviousStep(COMPANY_APPROVAL);

        #endregion
    }

}
