using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ContractPositionRequest
    {
        public BasePositionReference BasePosition { get; set; } = null!;
        public string Name { get; set; }
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public PersonReference AssignedPerson { get; set; } = null!;
        public string? Obs { get; set; }

        public double Workload { get; set; }


    }
}
