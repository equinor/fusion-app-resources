using Fusion.Resources.Database.Entities;
using System.Collections.Generic;

namespace Fusion.Resources.Logic.Workflows
{

    public class InternalRequestDirectWorkflowV1 : WorkflowDefinition
    {
        public const string CREATED = "created";
        public const string PROVISIONING = "provisioning";

        public override string Version => "v1";
        public override string Name => "Direct personnel assignment request";

        public InternalRequestDirectWorkflowV1()
            : base(null)
        {
            Steps = new List<WorkflowStep>()
            {
                Created,
                Provisioning
            };
        }

        public InternalRequestDirectWorkflowV1(DbPerson creator)
            : this()
        {
            Step(CREATED)
                .SetName("Created")
                .SetDescription($"{creator.Name} approved the request. The provisioning process will start so the person can access resources.")
                .Complete(creator, true)
                .StartNext();
        }

        public InternalRequestDirectWorkflowV1(DbWorkflow workflow)
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

        #endregion
    }

}
