using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiExternalPersonnelPerson
    {
        public ApiExternalPersonnelPerson(QueryExternalPersonnelPerson person)
        {
            Id = person.PersonnelId;
            AzureUniquePersonId = person.AzureUniqueId;
            Name = person.Name;
            FirstName = person.FirstName;
            LastName = person.LastName;
            JobTitle = person.JobTitle;
            PhoneNumber = person.PhoneNumber;
            Mail = person.Mail;
            AzureAdStatus = Enum.Parse<ApiAccountStatus>($"{person.AzureAdStatus}", true);
            Disciplines = person.Disciplines?.Select(d => new ApiPersonnelDiscipline(d)).ToList() ?? new List<ApiPersonnelDiscipline>();
        }

        /// <summary>
        /// The id for the personnel item. This item can be used across multiple contracts. 
        /// </summary>
        public Guid Id { get; set; }


        public Guid? AzureUniquePersonId { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string JobTitle { get; set; }
        public string PhoneNumber { get; set; }
        public string Mail { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiAccountStatus AzureAdStatus { get; set; }

        public bool HasCV { get; set; }

        public List<ApiPersonnelDiscipline> Disciplines { get; set; }
    }
}
