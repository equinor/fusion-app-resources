using System;

namespace Fusion.Resources.Api.Tests
{
    public class TestApiPersonnel
    {
        public Guid PersonnelId { get; set; }
        public Guid? AzureUniquePersonId { get; set; }
        public string UPN { get; set; }

        public string Mail { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string PreferredContactMail { get; set; }

        public bool? IsDeleted { get; set; }
        public DateTimeOffset? Deleted { get; set; }
    }
}
