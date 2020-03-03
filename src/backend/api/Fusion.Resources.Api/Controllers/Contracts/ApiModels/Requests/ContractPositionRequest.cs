using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ContractPositionRequest
    {
        public BasePositionReference BasePosition { get; set; }
        public string Name { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public PersonReference AssignedPerson { get; set; }

        public double Workload { get; set; }


    }
}
