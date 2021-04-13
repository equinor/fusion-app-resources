using System;

namespace Fusion.Resources.Application.LineOrg
{
    internal class ProfileWithDepartment
    {
        public Guid AzureUniqueId { get; set; }
        public string? Name { get; set; }
        public string? Mail { get; set; }
        public string FullDepartment { get; set; } = null!;
    }
}
