using Fusion.Resources.Domain;
using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiLocation
    {
        public ApiLocation(QueryLocation location)
        {
            Id = location.OrgLocationId;
            Name = location.Name;
            InternalId = location.Id;
        }

        public Guid Id { get; set; }
        public Guid InternalId { get; set; }
        public string Name { get; set; }
    }
}
