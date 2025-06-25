using Fusion.Integration.Profile;
using Fusion.Resources.Domain;
using System;
using System.Text.Json.Serialization;
using Fusion.Services.Org.ApiModels;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiPerson
    {


        public ApiPerson(Guid azureId, string mail)
        {
            AzureUniquePersonId = azureId;
            Mail = mail;
        }

        public ApiPerson(FusionPersonProfile profile)
        {
            AzureUniquePersonId = profile.AzureUniqueId;
            Mail = profile.Mail;
            Name = profile.Name;
            PhoneNumber = profile.MobilePhone;
            JobTitle = profile.JobTitle;
            AccountType = profile.AccountType;
            FullDepartment = profile.FullDepartment;
        }

        internal static ApiPerson? FromEntityOrDefault(ApiPersonV2? person)
        {
            if (person is not null)
                return new ApiPerson(person);
            return null;
        }

        internal static ApiPerson? FromEntityOrDefault(QueryPerson? person)
        {
            if (person != null)
                return new ApiPerson(person);
            return null;
        }

        public ApiPerson(QueryPerson person)
        {
            AzureUniquePersonId = person.AzureUniqueId;
            Mail = person.Mail;
            Name = person.Name;
            PhoneNumber = person.Phone;
            JobTitle = person.JobTitle;
            AccountType = person.AccountType;
        }

        public ApiPerson(ApiPersonV2 person)
        {
            AzureUniquePersonId = person.AzureUniqueId;
            Mail = person.Mail;
            Name = person.Name;
            PhoneNumber = person.MobilePhone;
            JobTitle = person.JobTitle;

            if (Enum.TryParse(person.AccountType, true, out FusionAccountType parsedAccountType))
                AccountType = parsedAccountType;
            else
                AccountType = FusionAccountType.External;
        }



        public Guid? AzureUniquePersonId { get; set; }
        public string? Mail { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? JobTitle { get; set; }

        public string? FullDepartment { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FusionAccountType AccountType { get; set; }
    }
}
