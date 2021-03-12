using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Logic.Workflows
{

    public class TaskOwnerChangeDirectWorkflowV1 : WorkflowDefinition
    {
        public const string CREATED = "created";
        public const string PROVISIONING = "provisioning";

        public override string Version => "v1";
        public override string Name => "Direct task owner change request";
        public override string TypeName => "directTaskOwnerChange";

        public TaskOwnerChangeDirectWorkflowV1()
            : base(null)
        {
            Steps = new List<WorkflowStep>()
            {
                Created,
                Provisioning
            };
        }

        public TaskOwnerChangeDirectWorkflowV1(DbPerson creator)
            : this()
        {
            Step(CREATED)
                .SetName("Created")
                .SetDescription($"{creator.Name} created the request. The provisioning process will start so changes are reflected in the org chart.")
                .Complete(creator, true)
                .StartNext();
        }

        public TaskOwnerChangeDirectWorkflowV1(DbWorkflow workflow)
            : base(workflow)
        {
        }
       
        #region Step definitions

        public static WorkflowStep Created => new WorkflowStep(CREATED, "Created")
            .WithDescription("Request was created and started.")
            .WithNextStep(PROVISIONING);
     
        public static WorkflowStep Provisioning => new WorkflowStep(PROVISIONING, "Provisioning")
            .WithDescription("If the request is approved, the new position or changes will be provisioned to the organisational chart.")
            .WithPreviousStep(CREATED);


        public override WorkflowStep? CompleteCurrentStep(DbWFStepState state, DbPerson user)
        {
            var current = GetCurrent();

            if (state == DbWFStepState.Rejected)
                throw new NotImplementedException("Rejected not supported");

            switch (current.Id)
            {
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

        #endregion
    }

}
