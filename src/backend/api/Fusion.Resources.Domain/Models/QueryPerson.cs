using System;
using Fusion.Integration.Profile;
using Fusion.Resources.Database.Entities;

namespace Fusion.Resources.Domain
{
    public class QueryPerson
    {
        public QueryPerson(DbPerson createdBy)
        {
            Id = createdBy.Id;
            Mail = createdBy.Mail;
            Name = createdBy.Name;
            AzureUniqueId = createdBy.AzureUniqueId;
            Phone = createdBy.Phone;
            JobTitle = createdBy.JobTitle;

            if (!Enum.TryParse<FusionAccountType>(createdBy.AccountType, true, out FusionAccountType type))
                throw new ArgumentException($"Cannot convert account type for user {createdBy.Mail}, using account type {createdBy.AccountType}");

            AccountType = type;
        }

        public Guid Id { get; set; }
        public Guid AzureUniqueId { get; set; }
        public string Mail { get; set; }
        public string Name { get; set; }
        public string JobTitle { get; set; }
        public string Phone { get; set; }
        public FusionAccountType AccountType { get; set; }

        public static QueryPerson FromEntityOrDefault(DbPerson updatedBy)
        {
            if (updatedBy != null)
                return new QueryPerson(updatedBy);
            return null;
        }
    }
}
