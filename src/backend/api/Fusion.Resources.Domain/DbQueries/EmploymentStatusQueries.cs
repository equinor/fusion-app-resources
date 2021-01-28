using System;
using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Fusion.Resources.Domain
{
    public static class EmploymentStatusQueries
    {
        public static IQueryable<DbPersonAbsence> GetById(this DbSet<DbPersonAbsence> table, PersonId identifier)
        {
            return identifier.Type switch
            {
                PersonId.IdentifierType.UniqueId => table.Where(c => c.PersonId == identifier.UniqueId || c.Person.AzureUniqueId == identifier.UniqueId),
                _ => table.Where(c => c.Person.Mail == identifier.Mail)
            };
        }

        public static IQueryable<DbPersonAbsence> GetById(this DbSet<DbPersonAbsence> table, PersonId identifier, Guid id)
        {
            var query = table.Where(p => p.Id == id);

            return identifier.Type switch
            {
                PersonId.IdentifierType.UniqueId => query.Where(c => c.PersonId == identifier.UniqueId || c.Person.AzureUniqueId == identifier.UniqueId),
                _ => query.Where(c => c.Person.Mail == identifier.Mail)
            };
        }
    }
}
