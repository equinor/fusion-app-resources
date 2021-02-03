using System;

namespace Fusion.Resources.Domain
{
    public class QueryLocation
    {
        public QueryLocation(Guid locationId)
        {
            Id = locationId;
            Name = "[Not resolved]";
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid OrgLocationId { get; set; }
    }
}

