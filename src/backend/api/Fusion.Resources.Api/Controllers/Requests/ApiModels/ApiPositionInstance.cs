using System;
using Fusion.ApiClients.Org;
using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiPositionInstance
    {
        public ApiPositionInstance(ApiPositionInstanceV2 instance)
        {
            Id = instance.Id;
            Workload = instance.Workload;
            Obs = instance.Obs;
            AppliesFrom = instance.AppliesFrom;
            AppliesTo = instance.AppliesTo;
            LocationId = instance.Location?.Id;

        }
     
        public Guid Id { get; set; }
        public double? Workload { get; set; }
        public string? Obs { get; set; } = null!;
        public DateTime AppliesFrom { get; set; }
        public DateTime AppliesTo { get; set; }
        public Guid? LocationId { get; set; }
    }
}