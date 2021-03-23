using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fusion.Resources.Logic.Workflows
{

    public class ContractorPersonnelWorkflowV1 : WorkflowDefinition
    {
        public const string CREATED = "created";
        public const string CONTRACTOR_APPROVAL = "contractorApproval";
        public const string COMPANY_APPROVAL = "companyApproval";

        public override string Version => "v1";
        public override string Name => "Contractor personnel request";

        public ContractorPersonnelWorkflowV1() 
            : base(null)
        {
            Steps = new List<WorkflowStep>()
            {
                Created,
                ContractorApproval,
                CompanyApproval,
                Provisioning
            };
        }

        public ContractorPersonnelWorkflowV1(DbPerson creator) 
            : this()
        {
            Step(CREATED)
                .SetDescription($"Request was created by {creator.Name}")
                .Start()
                .Complete(creator, true)
                .StartNext();
        }

        public ContractorPersonnelWorkflowV1(DbWorkflow workflow) 
            : base(workflow)
        {
        }


        public void ContractorApproved(DbPerson approver)
        {
            Step(CONTRACTOR_APPROVAL)
                .SetName("Submitted")
                .SetDescription($"{approver.Name} submitted the request for approval by company")
                .Complete(approver, true)
                .StartNext();
        }

        public void ContractorRejected(DbPerson editor, string reason)
        {
            Step(CONTRACTOR_APPROVAL)
                .SetName("Submitted")
                .SetDescription($"{editor.Name} rejected the request.")
                .SetReason(reason)
                .Complete(editor, false)
                .SkipRest()
                .CompleteWorkflow();
        }

        public void CompanyApproved(DbPerson approver)
        {
            Step(COMPANY_APPROVAL)
                .SetName("Approved")
                .SetDescription($"{approver.Name} approved the request. The provisioning process will start so the person can access contract resources.")
                .Complete(approver, true)
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

        public void ProvisionSuccessful(DbPerson systemAccount)
        {
            Step(PROVISIONING)
                .SetName("Provisioned")
                .SetDescription($"The request was successfully provisioned to the contract org chart.")
                .Complete(systemAccount, true)
                .CompleteWorkflow(); 
        }

        public void ProvisionError(string errorMessage)
        {
            Step(PROVISIONING)
                .SetName("Submitted")
                .SetDescription($"There was an error trying to provision the request to the contract org chart. " +
                    $"This could be a transient error. Please contact support if the issue is not resolved within a day.")
                .SetWorkflowError(errorMessage);
        }

        public override WorkflowStep? CompleteCurrentStep(DbWFStepState state, DbPerson user)
        {
            // This is not implemented for this workflow.
            throw new NotImplementedException();
        }

        #region Step definitions

        public static WorkflowStep Created => new WorkflowStep(CREATED, "Created")
            .WithDescription("Request was created and started.")
            .WithNextStep(CONTRACTOR_APPROVAL);

        public static WorkflowStep ContractorApproval => new WorkflowStep(CONTRACTOR_APPROVAL, "Submit")
            .WithDescription("Submit the personnel request to company for approval")
            .WithPreviousStep(CREATED)
            .WithNextStep(COMPANY_APPROVAL);

        public static WorkflowStep CompanyApproval => new WorkflowStep(COMPANY_APPROVAL, "Approve")
            .WithDescription("Review personnel request and approve/reject")
            .WithPreviousStep(CONTRACTOR_APPROVAL)
            .WithNextStep(PROVISIONING);

        public static WorkflowStep Provisioning => new WorkflowStep(PROVISIONING, "Provisioning")
            .WithDescription("If the request is approved, the new position or changes will be provisioned to the contract organisational chart.")
            .WithPreviousStep(COMPANY_APPROVAL);

        #endregion
    }

}
