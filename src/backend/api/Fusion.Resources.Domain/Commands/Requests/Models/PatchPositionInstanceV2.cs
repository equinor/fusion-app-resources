using System;
using System.Collections.Generic;
using Fusion.ApiClients.Org;
using Fusion.AspNetCore.Converters;
using Fusion.Integration.Core.Http.Patch;
using Newtonsoft.Json;

namespace Fusion.Resources.Domain.Commands
{
    public class PatchPositionInstanceV2 : PatchRequest
    {
        public PatchProperty<string?> ExternalId { get; set; } = null!;
        public PatchProperty<double?> Workload { get; set; } = null!;
        public PatchProperty<DateTime?> AppliesFrom { get; set; } = null!;
        public PatchProperty<DateTime?> AppliesTo { get; set; } = null!;
        public PatchProperty<Guid?> ParentPositionId { get; set; } = null!;
        public PatchProperty<List<Guid>> TaskOwnerIds { get; set; } = null!;
        public PatchProperty<string?> Obs { get; set; } = null!;
        public PatchProperty<bool> IsPrimary { get; set; } = null!;
        public PatchProperty<string?> Calendar { get; set; } = null!;
        public PatchProperty<string?> RotationId { get; set; } = null!;
        public PatchProperty<ApiPropertiesCollectionV2> Properties { get; set; } = null!;
        [JsonConverter(typeof(EnumStringValidationDefaultConverter))]
        public PatchProperty<ApiInstanceType?> Type { get; set; } = null!;
        public PatchProperty<ApiPositionLocationV2> Location { get; set; } = null!;
        public PatchProperty<ApiPersonV2> AssignedPerson { get; set; } = null!;
    }
}