using System;

#nullable enable

namespace Fusion.Testing.Mocks
{
    public class TestApiPerson
    {
        public Guid AzureUniquePersonId { get; set; }
        public Guid AzureUniqueId { get; set; }
        public string Name { get; set; } = null!;
        public string Mail { get; set; } = null!;
        public string? FullDepartment { get; set; }
    }


}
