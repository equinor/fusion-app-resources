using System;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public partial class ResourceAllocationRequest
    {
        public class QueryPositionInstance
        {
            public QueryPositionInstance()
            {
            }

            public QueryPositionInstance(
                DbResourceAllocationRequest.DbPositionInstance entity)
            {
                Id = entity.Id;
                Workload = entity.Workload;
                Obs = entity.Obs;
                AppliesFrom = entity.AppliesFrom;
                AppliesTo = entity.AppliesTo;
                Location = entity.Location;
            }

            public Guid Id { get; set; }
            public double Workload { get; set; }
            public string Obs { get; set; } = null!;
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public string? Location { get; set; }

        }
    }
}