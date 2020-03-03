using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public class QueryContractPersonnel
    {
        public QueryContractPersonnel(DbContractPersonnel item)
        {
            AzureUniqueId = item.Person.AzureUniqueId;
            Name = item.Person.Name;
            Mail = item.Person.Mail;
            PhoneNumber = item.Person.Phone;
            JobTitle = item.Person.JobTitle;
            AzureAdStatus = item.Person.AccountStatus;

            Created = item.Created;
            Updated = item.Updated;
            CreatedBy = new QueryPerson(item.CreatedBy);
            UpdatedBy = QueryPerson.FromEntityOrDefault(item.UpdatedBy);

            Disciplines = item.Person.Disciplines.Select(d => new QueryPersonnelDiscipline(d)).ToList();
        }
        public Guid? AzureUniqueId { get; set; }
        public string Name { get; set; }
        public string JobTitle { get; set; }
        public string PhoneNumber { get; set; }
        public string Mail { get; set; }

        public DbAzureAccountStatus AzureAdStatus { get; set; }

        public List<QueryPersonnelDiscipline> Disciplines { get; set; }

        public QueryPerson CreatedBy { get; set; }
        public QueryPerson UpdatedBy { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }

        public QueryProject Project { get; set; }
        public QueryContract Contract { get; set; }
    }
}
