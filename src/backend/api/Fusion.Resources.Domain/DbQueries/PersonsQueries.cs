using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Fusion.Resources.Domain
{
    public static class PersonsQueries
    {
        public static IQueryable<DbPerson> GetByPersonId(this DbSet<DbPerson> table, PersonId personId)
        {
            if (personId.Type != PersonId.IdentifierType.UniqueId)
                throw new InvalidOperationException("Cannot use mail as primary identifier for a person. Must resolve the user and use the unique object id");

            return table.Where(c => c.AzureUniqueId == personId.UniqueId);
        }
    }
}
