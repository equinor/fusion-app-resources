using Fusion.Resources.Domain;
using Newtonsoft.Json;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiProjectReference
    {
        public ApiProjectReference(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public ApiProjectReference(QueryProject project)
        {
            Id = project.OrgProjectId;
            Name = project.Name;
            InternalId = project.Id;
            State = project.State;
        }

        public ApiProjectReference(QueryProjectRef project)
        {
            Id = project.OrgProjectId;
            Name = project.Name;
        }

        /// <summary>
        /// The internal id.
        /// </summary>
        public Guid Id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Guid? InternalId { get; set; }
        public string Name { get; set; }
        public string? State { get; set; }
    }
}
