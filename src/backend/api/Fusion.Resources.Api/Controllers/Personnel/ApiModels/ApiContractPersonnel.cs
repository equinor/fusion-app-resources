using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiContractPersonnel
    {
        public Guid? AzureUniquePersonId { get; set; }
        public string Name { get; set; }
        public string JobTitle { get; set; }
        public string PhoneNumber { get; set; }
        public string Mail { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ApiAccountStatus AzureAdStatus { get; set; }
        
        public bool HasCV { get; set; }

        public List<PersonnelDiscipline> Disciplines { get; set; }

        public enum ApiAccountStatus { Available, Invited, NoAccount }
    }

    public class PersonnelDiscipline
    {
        public string Name { get; set; }
        //public bool IsVerifiedByCompany { get; set; }
    }


}
