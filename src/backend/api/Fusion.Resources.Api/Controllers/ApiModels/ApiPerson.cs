using Fusion.Integration.Profile;
using System;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiPerson
    {
        public ApiPerson() { }
        public ApiPerson(FusionFullPersonProfile profile)
        {
            AzureUniquePersonId = profile.AzureUniqueId;
            Mail = profile.Mail;
            Name = profile.Name;
            PhoneNumber = profile.MobilePhone;
            JobTitle = profile.JobTitle;
            AccountType = profile.AccountType;
        }

        public Guid? AzureUniquePersonId { get; set; }
        public string Mail { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string JobTitle { get; set; } = null!;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FusionAccountType AccountType { get; set; }
    }
}
