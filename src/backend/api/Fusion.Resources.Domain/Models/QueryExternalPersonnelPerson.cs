using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Domain
{
    public class QueryExternalPersonnelPerson
    {
        public QueryExternalPersonnelPerson(DbExternalPersonnelPerson item)
        {
            PersonnelId = item.Id;

            AzureUniqueId = item.AzureUniqueId;
            Name = item.Name;
            FirstName = item.FirstName;
            LastName = item.LastName;
            Mail = item.Mail;
            PhoneNumber = item.Phone;
            JobTitle = item.JobTitle;
            AzureAdStatus = item.AccountStatus;
            DawinciCode = item.DawinciCode;
            LinkedInProfile = item.LinkedInProfile;

            if (item.Disciplines == null)
                throw new ArgumentNullException(nameof(item.Disciplines), "Disciplines must be included or initialized on the entity when constructing query model");

            Disciplines = item.Disciplines.Select(d => new QueryPersonnelDiscipline(d)).ToList();
        }
        public Guid PersonnelId { get; set; }
        public Guid? AzureUniqueId { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? JobTitle { get; set; }
        public string PhoneNumber { get; set; }
        public string Mail { get; set; }
        public string? DawinciCode { get; set; }
        public string? LinkedInProfile { get; set; }

        public DbAzureAccountStatus AzureAdStatus { get; set; }

        public List<QueryPersonnelDiscipline> Disciplines { get; set; }
    }
}
