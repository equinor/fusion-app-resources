using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiProjectReference
    {
        public ApiProjectReference()
        {
        }
        public ApiProjectReference(QueryProject project)
        {
            Id = project.OrgProjectId;
            Name = project.Name;
            InternalId = project.Id;
        }

        /// <summary>
        /// The internal id.
        /// </summary>
        public Guid Id { get; set; }

        public Guid InternalId { get; set; }
        public string Name { get; set; }

        public Guid ProjectMasterId { get; set; }
    }
}
