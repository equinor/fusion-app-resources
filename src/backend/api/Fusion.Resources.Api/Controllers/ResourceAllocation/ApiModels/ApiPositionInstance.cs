using System;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiPositionInstance
    {
        public ApiPositionInstance(ResourceAllocationRequest.QueryPositionInstance query)
        {
            Id = query.Id;
            Workload = query.Workload;
            Obs = query.Obs;
            AppliesFrom = query.AppliesFrom;
            AppliesTo = query.AppliesTo;
            Location = query.Location;
            
        }
        
        public Guid Id { get; set; }
        public double Workload { get; set; }
        public string Obs { get; set; } = null!;
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public string? Location { get; set; }
    }
}