using Fusion.Resources.Database.Entities;
using System.Linq;

namespace Fusion.Resources.Domain
{
    public static class ExternalPersonnelQueries
    {
        public static IQueryable<DbExternalPersonnelPerson> GetById(this IQueryable<DbExternalPersonnelPerson> query, PersonnelId identifier)
        {
            return identifier.Type switch
            {
                PersonnelId.IdentifierType.UniqueId => query.Where(c => c.Id == identifier.UniqueId || c.AzureUniqueId == identifier.UniqueId),
                _ => query.Where(c => c.Mail == identifier.Mail)
            };
        }
    }
}
