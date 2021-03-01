using System;
using System.Collections.Generic;
using Fusion.ApiClients.Org;
using Fusion.AspNetCore.Api;
using Fusion.AspNetCore.Converters;
using Newtonsoft.Json;

namespace Fusion.Testing.Mocks.OrgService.Api.Controllers
{
    public class PatchInstanceRequestV2 : PatchRequest
    {
        public PatchProperty<string> ExternalId { get; set; }

        public PatchProperty<double?> Workload { get; set; }

        public PatchProperty<DateTime?> AppliesFrom { get; set; }

        public PatchProperty<DateTime?> AppliesTo { get; set; }

        public PatchProperty<Guid?> ParentPositionId { get; set; }

        public PatchProperty<List<Guid>> TaskOwnerIds { get; set; }

        public PatchProperty<string> Obs { get; set; }

        public PatchProperty<bool> IsPrimary { get; set; }

        public PatchProperty<string> Calendar { get; set; }

        public PatchProperty<string> RotationId { get; set; }

        public PatchProperty<Dictionary<string, object>> Properties { get; set; }


        /// <summary>
        /// The type is normal by default.
        /// Using custom converter to allow type property to be null when parsing the model.
        /// </summary>
        [JsonConverter(typeof(EnumStringValidationDefaultConverter))]
        public PatchProperty<ApiInstanceType?> Type { get; set; }

        public PatchProperty<LocationRef> Location { get; set; }

        public PatchProperty<PersonRef> AssignedPerson { get; set; }
    }

    public class LocationRef
    {
        public Guid Id { get; set; }
    }

    public class PersonRef
    {
        public Guid? AzureUniqueId { get; set; }

        public string Mail { get; set; }

        public static implicit operator PersonIdentifier(PersonRef personRef)
        {
            return new PersonIdentifier(personRef.AzureUniqueId!.Value);
        }
    }
}