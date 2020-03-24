using System;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ContractorPersonnelRequest
    {
        public class PositionInfo
        {
            public Guid BasePositionId { get; set; }
            public string PositionName { get; set; } = null!;
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public double Workload { get; set; }
            public string? Obs { get; set; }
        }

    }


}
