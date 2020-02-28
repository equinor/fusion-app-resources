using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ContractPersonnelRequestRequest
    {
        public Guid? Id { get; set; }
        public string Description { get; set; }

        public RequestPosition Position { get; set; }
        public PersonReference Person { get; set; }
    

        public class RequestPosition
        {
            /// <summary>
            /// Existing org chart position id.
            /// </summary>
            public Guid? Id { get; set; }

            public BasePositionReference BasePosition { get; set; }
            public string Name { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public string Obs { get; set; }
            
            public TaskOwnerReference TaskOwner { get; set; }
        }


    }

}
