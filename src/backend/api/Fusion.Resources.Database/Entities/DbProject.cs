using System;
using System.Collections.Generic;

namespace Fusion.Resources.Database.Entities
{
    public class DbProject
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? DomainId { get; set; }
        public Guid OrgProjectId { get; set; }
        public string? State { get; set; }
    }


}
