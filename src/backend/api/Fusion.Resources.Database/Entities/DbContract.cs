using System;

namespace Fusion.Resources.Database.Entities
{
    public class DbContract
    {
        public Guid Id { get; set; }
        public string ContractNumber { get; set; }
        public Guid OrgContractId { get; set; }
        public string Name { get; set; }
        
        public DbProject Project { get; set; }
        public Guid ProjectId { get; set; }

        public DateTimeOffset Allocated { get; set; }
        public DbPerson AllocatedBy { get; set; }
        public Guid AllocatedById { get; set; }
    }

}
