using Fusion.Resources.Database.Entities;
using System.Collections.Generic;

namespace Fusion.Resources.Logic.Workflows
{

    public class InternalRequestJointVentureWorkflowV1 : WorkflowDefinition
    {
        public const string CREATED = "created";
        public const string APPROVAL = "approval";
        public const string PROVISIONING = "provisioning";

        public override string Version => "v1";
        public override string Name => "Joint venture personnel assignment request";

        public InternalRequestJointVentureWorkflowV1()
            : base(null)
        {
            Steps = new List<WorkflowStep>()
            {
                Created,
                Approval,
                Provisioning
            };
        }

        public InternalRequestJointVentureWorkflowV1(DbPerson creator)
            : this()
        {
            Step(CREATED)
                .SetDescription($"Request was created by {creator.Name}")
                .Start()
                .Complete(creator, true)
                .StartNext();
        }

        public InternalRequestJointVentureWorkflowV1(DbWorkflow workflow)
            : base(workflow)
        {
        }

        public void Approved(DbPerson approver)
        {
            Step(APPROVAL)
                .SetName("Approved")
                .SetDescription($"{approver.Name} approved the request. The provisioning process will start so the person can access resources.")
                .Complete(approver, true)
                .StartNext();
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
