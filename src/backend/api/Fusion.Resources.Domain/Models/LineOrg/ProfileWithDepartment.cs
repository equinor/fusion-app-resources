using System;

namespace Fusion.Resources.Domain.LineOrg
{
    public class ProfileWithDepartment
    {
        public Guid AzureUniqueId { get; set; }
        public string Name { get; set; }
        public string Mail { get; set; }
        public string FullDepartment { get; set; }
    }
}
