using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Fusion.Resources.Logic.Commands;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiExternalPersonnelPerson
    {
        public ApiExternalPersonnelPerson(QueryExternalPersonnelPerson person)
        {
            Id = person.PersonnelId;
            AzureUniquePersonId = person.AzureUniqueId;
            UPN = person.UPN;
            Name = person.Name;
            FirstName = person.FirstName;
            LastName = person.LastName;
            JobTitle = person.JobTitle;
            PhoneNumber = person.PhoneNumber;
            Mail = person.Mail;
            DawinciCode = person.DawinciCode;
            LinkedInProfile = person.LinkedInProfile;
            PreferredContactMail = person.PreferredContactMail;
            AzureAdStatus = Enum.Parse<ApiAccountStatus>($"{person.AzureAdStatus}", true);
            IsDeleted = person.IsDeleted;
            Deleted = person.Deleted;
            Disciplines = person.Disciplines?.Select(d => new ApiPersonnelDiscipline(d)).ToList() ?? new List<ApiPersonnelDiscipline>();
        }

        /// <summary>
        /// The id for the personnel item. This item can be used across multiple contracts. 
        /// </summary>
        public Guid Id { get; set; }


        public Guid? AzureUniquePersonId { get; set; }
        public string? UPN { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string? JobTitle { get; set; }
        public string PhoneNumber { get; set; }
        public string Mail { get; set; }
        public string? DawinciCode { get; set; }
        public string? LinkedInProfile { get; set; }
        public string? PreferredContactMail { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiAccountStatus AzureAdStatus { get; set; }

        public bool HasCV { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset? Deleted { get; set; }

        public List<ApiPersonnelDiscipline> Disciplines { get; set; }
    }
}
