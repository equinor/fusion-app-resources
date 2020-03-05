using System;
using System.Collections.Generic;

namespace Fusion.Resources.Database.Entities
{
    public class DbProject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string DomainId { get; set; }
        public Guid OrgProjectId { get; set; }

        public ICollection<DbContract> Contracts { get; set; }
    }


}
