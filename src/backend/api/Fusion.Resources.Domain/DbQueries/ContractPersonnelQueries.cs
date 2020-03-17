using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fusion.Resources.Domain
{
    public static class ContractPersonnelQueries
    {
        public static IQueryable<DbContractPersonnel> GetById(this DbSet<DbContractPersonnel> table, PersonnelId identifier) 
        {
            return identifier.Type switch
            {
                PersonnelId.IdentifierType.UniqueId => table.Where(c => c.PersonId == identifier.UniqueId || c.Person.AzureUniqueId == identifier.UniqueId),
                _ => table.Where(c => c.Person.Mail == identifier.Mail)
            };
        }

        public static IQueryable<DbContractPersonnel> GetById(this DbSet<DbContractPersonnel> table, Guid orgContractId, PersonnelId identifier)
        {
            var query = table.Where(p => p.Contract.OrgContractId == orgContractId);

            return identifier.Type switch
            {
                PersonnelId.IdentifierType.UniqueId => query.Where(c => c.PersonId == identifier.UniqueId || c.Person.AzureUniqueId == identifier.UniqueId),
                _ => query.Where(c => c.Person.Mail == identifier.Mail)
            };
        }
    }
}
