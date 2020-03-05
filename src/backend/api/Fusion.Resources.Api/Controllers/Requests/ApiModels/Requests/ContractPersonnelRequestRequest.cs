using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ContractPersonnelRequestRequest
    {
        public Guid? Id { get; set; }
        public string Description { get; set; } = null!;

        public RequestPosition Position { get; set; } = null!;
        public PersonReference Person { get; set; } = null!;
    

        public class RequestPosition
        {
            /// <summary>
            /// Existing org chart position id.
            /// </summary>
            public Guid? Id { get; set; }

            public BasePositionReference BasePosition { get; set; } = null!;
            public string Name { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public string Obs { get; set; }

            public TaskOwnerReference TaskOwner { get; set; } = null!;
            public double Workload { get; set; }
        }


    }

}
