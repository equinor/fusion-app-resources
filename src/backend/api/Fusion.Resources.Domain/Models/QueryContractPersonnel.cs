using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Resources.Database.Entities;

#nullable enable

namespace Fusion.Resources.Domain
{
    public class QueryContractPersonnel
    {
        public QueryContractPersonnel(DbContractPersonnel item)
        {
            Id = item.Id;

            PersonnelId = item.PersonId;

            AzureUniqueId = item.Person.AzureUniqueId;
            Name = item.Person.Name;
            FirstName = item.Person.FirstName;
            LastName = item.Person.LastName;
            Mail = item.Person.Mail;
            PhoneNumber = item.Person.Phone;
            JobTitle = item.Person.JobTitle;
            AzureAdStatus = item.Person.AccountStatus;
            DawinciCode = item.Person.DawinciCode;
            LinkedInProfile = item.Person.LinkedInProfile;
            PreferredContactMail = item.Person.PreferredContractMail;

            Created = item.Created;
            Updated = item.Updated;
            CreatedBy = new QueryPerson(item.CreatedBy);
            UpdatedBy = QueryPerson.FromEntityOrDefault(item.UpdatedBy);

            Disciplines = item.Person.Disciplines.Select(d => new QueryPersonnelDiscipline(d)).ToList();
        }

        /// <summary>
        /// The id for the personnel relation to this contract.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The id of the personnel item. This item can be used across multiple contracts.
        /// </summary>
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
        public string? PreferredContactMail { get; set; }

        public DbAzureAccountStatus AzureAdStatus { get; set; }

        public List<QueryPersonnelDiscipline> Disciplines { get; set; }

        public QueryPerson CreatedBy { get; set; }
        public QueryPerson? UpdatedBy { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }

        //public QueryProject Project { get; set; }
        //public QueryContract Contract { get; set; }


        public List<QueryOrgPositionInstance>? Positions { get; set; }
        public List<QueryPersonnelRequestReference>? Requests { get; set; }
    }
}
