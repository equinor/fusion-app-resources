using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Logic.Workflows
{

    public class ResourceOwnerChangeWorkflowV1 : WorkflowDefinition
    {
        public const string CREATED = "created";
        public const string ACCEPTANCE = "acceptance";

        public override string Version => "v1";
        public override string Name => "Change request from the resource owner";

        public ResourceOwnerChangeWorkflowV1()
            : base(null)
        {
            Steps = new List<WorkflowStep>()
            {
                Created,
                Acceptance,
                Provisioning
            };
        }

        public ResourceOwnerChangeWorkflowV1(DbPerson creator)
            : this()
        {
            Step(CREATED)
                .SetDescription($"Request was created by {creator.Name}")
                .Start()
                .Complete(creator, true)
                .StartNext();
        }

        public ResourceOwnerChangeWorkflowV1(DbWorkflow workflow)
            : base(workflow)
        {
        }

        public WorkflowStep Accepted(DbPerson approver)
        {
            return Step(ACCEPTANCE)
                .SetName("Accepted")
                .SetDescription($"{approver.Name} accepted the changes. The provisioning process will start so changes are visible in the org chart.")
                .Complete(approver, true)
                .StartNext().Current;
        }

        public override WorkflowStep? CompleteCurrentStep(DbWFStepState state, DbPerson user)
        {
            var current = GetCurrent();

            if (state == DbWFStepState.Rejected)
                throw new NotImplementedException("Rejected not supported");

            switch (current.Id)
            {
                case ACCEPTANCE:
                    return Accepted(user);

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
            .WithNextStep(ACCEPTANCE);

        public static WorkflowStep Acceptance => new WorkflowStep(ACCEPTANCE, "Accept")
            .WithDescription("Review personnel request and accept changes")
            .WithPreviousStep(CREATED)
            .WithNextStep(PROVISIONING);

        public static WorkflowStep Provisioning => new WorkflowStep(PROVISIONING, "Provisioning")
            .WithDescription("If the request is approved, the new position or changes will be provisioned to the organisational chart.")
            .WithPreviousStep(ACCEPTANCE);

        #endregion
    }

}
