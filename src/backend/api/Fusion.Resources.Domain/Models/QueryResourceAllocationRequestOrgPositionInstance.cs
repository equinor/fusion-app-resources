﻿using System;
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
                DbResourceAllocationRequest.DbOpPositionInstance entity)
            {
                Id = entity.Id;
                Workload = entity.Workload;
                Obs = entity.Obs;
                AppliesFrom = entity.AppliesFrom;
                AppliesTo = entity.AppliesTo;
                LocationId = entity.LocationId;
            }

            public Guid Id { get; set; }
            public double? Workload { get; set; }
            public string? Obs { get; set; } = null!;
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public Guid? LocationId { get; set; }

            public DbResourceAllocationRequest.DbOpPositionInstance ToEntity()
            {
                return new()
                {
                    Id = Id,
                    AppliesFrom = AppliesFrom,
                    AppliesTo = AppliesTo,
                    LocationId = LocationId,
                    Workload = Workload,
                    Obs = Obs
                };
            }
        }
    }
}